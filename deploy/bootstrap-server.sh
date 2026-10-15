#!/usr/bin/env bash
set -euo pipefail

readonly DEPLOY_USER=deploy
readonly APP_DIR=/opt/drivinglessons
readonly BACKUP_DIR=/var/backups/drivinglessons
readonly SWAP_FILE=/swapfile
readonly SWAP_SIZE=1G
readonly SSHD_DROP_IN=/etc/ssh/sshd_config.d/99-drivinglessons.conf

if [[ $EUID -ne 0 ]]; then
    echo "Run with sudo." >&2
    exit 1
fi

if [[ $# -lt 1 ]]; then
    echo "Usage: sudo ./bootstrap-server.sh '<ssh public key>' ['<ssh public key>' ...]" >&2
    exit 1
fi

export DEBIAN_FRONTEND=noninteractive

echo "== packages and automatic security updates"
apt-get update
apt-get -y upgrade
apt-get -y install ca-certificates curl unattended-upgrades
printf 'APT::Periodic::Update-Package-Lists "1";\nAPT::Periodic::Unattended-Upgrade "1";\n' \
    > /etc/apt/apt.conf.d/20auto-upgrades
timedatectl set-timezone UTC

echo "== docker engine and compose plugin"
install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
chmod a+r /etc/apt/keyrings/docker.asc
# shellcheck source=/dev/null
. /etc/os-release
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu ${VERSION_CODENAME} stable" \
    > /etc/apt/sources.list.d/docker.list
apt-get update
apt-get -y install docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
systemctl enable --now docker

echo "== swap"
if ! swapon --show | grep -q "$SWAP_FILE"; then
    fallocate -l "$SWAP_SIZE" "$SWAP_FILE"
    chmod 600 "$SWAP_FILE"
    mkswap "$SWAP_FILE"
    swapon "$SWAP_FILE"
    echo "$SWAP_FILE none swap sw 0 0" >> /etc/fstab
fi

echo "== deploy user"
if ! id "$DEPLOY_USER" > /dev/null 2>&1; then
    adduser --disabled-password --gecos "" "$DEPLOY_USER"
fi
usermod -aG docker "$DEPLOY_USER"
install -d -m 700 -o "$DEPLOY_USER" -g "$DEPLOY_USER" "/home/$DEPLOY_USER/.ssh"
printf '%s\n' "$@" > "/home/$DEPLOY_USER/.ssh/authorized_keys"
chown "$DEPLOY_USER:$DEPLOY_USER" "/home/$DEPLOY_USER/.ssh/authorized_keys"
chmod 600 "/home/$DEPLOY_USER/.ssh/authorized_keys"

echo "== ssh: keys only"
printf 'PasswordAuthentication no\nKbdInteractiveAuthentication no\nPermitRootLogin no\n' > "$SSHD_DROP_IN"
install -d -m 755 /run/sshd
sshd -t
systemctl try-reload-or-restart ssh

echo "== directories"
install -d -m 750 -o "$DEPLOY_USER" -g "$DEPLOY_USER" "$APP_DIR" "$BACKUP_DIR"

echo "== aws cli"
if ! snap list aws-cli > /dev/null 2>&1; then
    snap install aws-cli --classic
fi

echo "== done: docker $(docker --version | cut -d' ' -f3 | tr -d ,), compose $(docker compose version --short)"
