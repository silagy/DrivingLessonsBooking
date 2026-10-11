# AWS Account Setup

A step-by-step guide to opening a new AWS account and making it safe to use **before** anything is deployed. Once this is done, continue with the production setup in the deployment plan ([#74](https://github.com/silagy/DrivingLessonsBooking/issues/74), `docs/modules/deployment/us-74-production-deployment-plan/`, task 8), which registers the domain and creates the server. The overview of the whole path to production is [deploying-to-production.md](deploying-to-production.md).

Time: about 45 minutes of clicking, plus up to 24 hours for AWS to activate the account. Console labels move around occasionally; if a button is named slightly differently, use the search bar at the top of the console.

## 0. Decide who owns the account

This is a decision, not a technical step. Make it first, because moving resources between accounts later is painful.

| Option | Root email and card | Fits when |
|---|---|---|
| **The school owns it** (recommended) | The owner's business email and card; you get your own admin login | The school pays AWS directly, and the system stays with the school if you step away |
| **You own it** | Your email and card; you invoice the school | You manage several clients and want one bill |

Whichever you choose, use an email address that will **outlive any one person or job**. A shared mailbox or alias works well, for example `aws@<school domain>` or a dedicated Gmail like `<school>.aws@gmail.com`. Whoever controls that mailbox controls the account.

## 1. Have these ready

- The root email address from step 0, with access to its inbox.
- A mobile phone that can receive an SMS or a voice call.
- A credit or debit card. AWS places a temporary authorization of about $1 on it.
- An authenticator app on your phone (Google Authenticator, Microsoft Authenticator, 1Password, ...) **or** a passkey/security key.
- A password manager to store the root password and recovery details.

## 2. Create the account

1. Go to **https://aws.amazon.com** → **Create an AWS account**.
2. **Root user email address**: the address from step 0. **AWS account name**: `drivinglessons-prod`.
3. Enter the verification code AWS emails you.
4. Set a **long, unique root password** (generate it in your password manager).
5. **Contact information**:
   - *Business* if the school owns the account (school name, business address in Israel, phone).
   - *Personal* if you own it.
   - Accept the AWS Customer Agreement.
6. **Billing information**: the card. Choose to be charged in USD unless you prefer otherwise.
7. **Identity verification**: choose SMS or voice call, enter the phone number, then type the code.
8. **Support plan**: **Basic support - Free**. (Basic still covers account and billing questions, which is all this project needs.)
9. **Complete sign up**. You'll get a "Welcome to Amazon Web Services" email. Activation is usually minutes, but can take up to 24 hours. Until it's active, some services refuse to start.

## 3. Lock down the root user

The root user can do everything, including closing the account. You'll use it only for this step and for rare account-level tasks.

1. Sign in at **https://console.aws.amazon.com** → **Root user** → the root email and password.
2. Top-right menu (account name) → **Security credentials**.
3. **Multi-factor authentication (MFA)** → **Assign MFA device**:
   - Name it, e.g. `root-phone`.
   - Choose **Passkey or security key** or **Authenticator app**.
   - Follow the prompts (for an app: scan the QR code, then enter two consecutive codes).
4. Add a **second** MFA device if you have one (e.g. a security key plus the phone app). AWS allows several, and it saves you if a phone is lost.
5. On the same page, check that **Access keys** shows **none**. Never create access keys for the root user.
6. Save the account ID (12 digits, top-right menu) in your password manager next to the root credentials.

## 4. Billing guardrails

Do this before creating anything that costs money.

1. Search **Billing and Cost Management**.
2. **Budgets** → **Create budget**:
   - Choose **Use a template** → **Monthly cost budget**.
   - Name: `monthly-limit`. Amount: **$20** (the system should cost about $13-15 a month).
   - Email recipients: the root email **and** your own email.
   - **Create budget**. AWS emails you when actual spend passes 85% and 100%, and when forecast spend is heading over.
3. **Billing preferences** → **Alert preferences** → **Edit** → tick **Receive AWS Free Tier alerts**, enter your email, then **Update**.
4. **Account** (top-right menu → **Account**) → scroll to **IAM user and role access to Billing information** → **Edit** → **Activate IAM Access** → **Update**. Without this, the admin user you create next cannot see the bill.
5. On the same Account page, under **Alternate contacts**, add yourself as the **Billing**, **Operations** and **Security** contact, so AWS notices don't go only to the root mailbox.

## 5. Create your everyday admin login

Never work as root day to day. Create one admin user for yourself (and later one for anyone else who needs access).

1. Search **IAM** → **Users** → **Create user**.
2. User name: `<your-first-name>-admin`. Tick **Provide user access to the AWS Management Console** → **I want to create an IAM user**. Set a password of your own (custom password), untick "must create a new password at next sign-in". **Next**.
3. **Attach policies directly** → tick **AdministratorAccess**. **Next** → **Create user**.
4. On the confirmation screen, copy the **Console sign-in URL** (`https://<account-id>.signin.aws.amazon.com/console`) into your password manager.
5. Make the sign-in URL readable: IAM → **Dashboard** → **AWS Account** panel → **Account alias** → **Create** → `drivinglessons-prod`. The URL becomes `https://drivinglessons-prod.signin.aws.amazon.com/console`.
6. **Sign out of root.** Sign in through the new URL as `<your-first-name>-admin`.
7. Top-right menu → **Security credentials** → **Assign MFA device** (same as step 3, but for this user). From here on, every sign-in needs the password and MFA.

(AWS also offers **IAM Identity Center** for single sign-on across many accounts. With one account and one or two people it adds steps without benefit, so a plain IAM user with MFA is the right size here.)

## 6. Set the region

Everything in this project lives in **Europe (Frankfurt) `eu-central-1`**: the server, SES email and the backup bucket.

- Top-right region menu → **Europe (Frankfurt) eu-central-1**. The console remembers it.
- Some services ignore the region, which is expected: **IAM**, **Route 53** (domains and DNS) and **Billing** always show "Global".
- **Lightsail** has its own region picker inside its console. Choose Frankfurt there too.

## 7. Optional: the AWS CLI on your PC

You only need it if you prefer typing to clicking. For example, the runbook's restore section can download a backup either from the S3 console or with the CLI.

1. Install: `winget install -e --id Amazon.AWSCLI` (or the MSI from https://aws.amazon.com/cli/), then open a new terminal and run `aws --version`.
2. Credentials for your admin user: IAM → Users → `<your-first-name>-admin` → **Security credentials** → **Create access key** → **Command Line Interface (CLI)** → confirm. Copy both values **now**, because the secret is shown only once.
3. Run `aws configure --profile drivinglessons-admin`: paste the key and secret, region `eu-central-1`, output `json`.
4. Test: `aws sts get-caller-identity --profile drivinglessons-admin` prints your account ID and user.

Treat that access key like a password: it's never committed and never pasted into chat. Delete it in IAM if your PC is lost.

## 8. Check before moving on

- [ ] The root user has MFA and no access keys. Root password, account ID and recovery details are in the password manager.
- [ ] The `monthly-limit` budget exists and its emails reach you.
- [ ] IAM access to billing is activated, and alternate contacts are set.
- [ ] You sign in daily as `<your-first-name>-admin` with MFA, through the account-alias URL.
- [ ] The console region is Frankfurt `eu-central-1`.
- [ ] (Optional) `aws sts get-caller-identity --profile drivinglessons-admin` works.

## 9. What it will cost

Rough monthly figures (always check the pricing pages; prices change):

| Item | Approximate cost |
|---|---|
| Lightsail 2 GB instance (static IP included while attached) | ~$12/month |
| Route 53 hosted zone | $0.50/month |
| Domain registration | ~$13-15/year for `.com` (varies by TLD) |
| SES email (a few emails a week) | cents |
| S3 backups (30 small dumps) | cents |
| **Total** | **about $13-15/month** plus the yearly domain |

## 10. Next

Continue with task 8 of the deployment plan, Steps 1-3: register the domain, start SES verification and the production-access request (these take time, so start them first), then create the Lightsail instance. Two things that sometimes catch new accounts:

- **Domain registration or the SES production-access request is refused or "pending"** on a brand-new account. Open a case in **Support Center** → **Account and billing** (free with Basic support) and ask for the account to be verified for Route 53 registration / SES production use.
- **Lightsail says the account is not yet activated.** Wait for the activation email from step 2, or check **Billing** → **Payment methods** to see whether the card was declined.
