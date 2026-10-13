# AWS Account Setup

A step-by-step guide to opening a new AWS account and making it safe to use **before** anything is deployed. Once this is done, continue with the production setup in the deployment plan ([#74](https://github.com/silagy/DrivingLessonsBooking/issues/74), `docs/modules/deployment/us-74-production-deployment-plan/`, task 8), which registers the domain and creates the server. The overview of the whole path to production is [deploying-to-production.md](deploying-to-production.md).

Time: about an hour of clicking, plus up to 24 hours for AWS to activate the account. Console labels move around occasionally; if a button is named slightly differently, use the search bar at the top of the console.

> **Which sign-up did you get?** New AWS sign-ups now land in a simplified **projects** experience: the console header shows a project menu, Console Home has **Manage projects** / **Manage team** buttons, and there is **no region selector**. AWS picks the region from your contact address and, for Israel, that's Europe (Stockholm) `eu-north-1`, not the Frankfurt this project uses. This guide follows that path and turns on the full console in section 3. If your console already has a region selector at the top right, you have a classic account: follow the [appendix](#appendix-a-classic-account) instead.

## 0. Decide who owns the account

This is a decision, not a technical step. Make it first, because moving resources between accounts later is painful.

| Option | Sign-in, root email and card | Fits when |
|---|---|---|
| **The school owns it** (recommended) | The owner's business email and card; you're invited as a team member | The school pays AWS directly, and the system stays with the school if you step away |
| **You own it** | Your email and card; you invoice the school | You manage several clients and want one bill |

Two identities end up controlling everything: the **AWS Builder ID** you sign up with (daily sign-in and AWS Settings) and the **management account root email** chosen in section 3 (recovery). Use addresses that will **outlive any one person or job**: a shared mailbox or alias, for example `aws@<school domain>`, or a dedicated Gmail like `<school>.aws@gmail.com`. Whoever controls those mailboxes controls the account.

## 1. Have these ready

- The email address from step 0, with access to its inbox.
- A mobile phone that can receive an SMS or a voice call.
- A credit or debit card. AWS places a temporary authorization of about $1 on it.
- An authenticator app on your phone (Google Authenticator, Microsoft Authenticator, 1Password, ...) **or** a passkey/security key.
- A password manager to store passwords and recovery details.

## 2. Create the account

1. Go to **https://aws.amazon.com** → **Create an AWS account**, and sign up with an **AWS Builder ID** on the email from step 0.
2. Fill in contact details (business details if the school owns it), the card, and phone verification as prompted. Choose USD billing unless you prefer otherwise.
3. Name the first project, for example `drivinglessons-prod` (any name works; this guide calls it "the project"). The project is an AWS account of its own; everything this system runs (server, DNS, email, backups) goes in it.
4. Wait for the welcome email. Activation is usually minutes, but can take up to 24 hours.

## 3. Activate advanced features

The simplified experience locks you to one region, limits the services you can use and replaces budgets with a spend limit. Activating advanced features gives you the normal console with a region selector, every service and full billing tools. **It cannot be reversed**, and you become the administrator of the AWS Organization that holds your projects.

1. Open **https://settings.aws.com** → **Projects** → **Actions** → **Explore advanced features**.
2. **Team name**: anything your team members may see, e.g. `<school>-team`.
3. **Management account email**: keep **Use my AWS Builder ID email** if that mailbox is the long-lived one from step 0; otherwise choose **Use a different email address** and enter it. This becomes the root user email of the management account. **Send verification code**, enter it.
4. **Review and complete activation** → confirm.

When it finishes, AWS Settings → **Projects** lists your project and a new **`<team name>` Identity Delegated Admin** project, each with an **AWS Console** button, and below them **Management accounts** → **`<team name>` Management Account** → **Centrally manage your organization**. What each one is for:

| Account | What it's for | You use it for |
|---|---|---|
| **Management account** | Owns the organization and the bill | Billing, budgets, organization policies. Nothing else. |
| **Project account** (your project) | A member account of the organization | **Everything the system runs**: Lightsail, Route 53, SES, S3 |
| **`<team name>` Identity Delegated Admin** | Created by AWS to manage team members | Inviting and removing people (IAM Identity Center) |

The project spend limit is removed at this moment, so do section 5 (the budget) the same day.

## 4. Lock down the sign-ins

### Management account root user

It can do everything, including closing the organization. You set its password once and use it only for recovery.

1. Go to **https://console.aws.amazon.com** → **Sign in using root user email** → the management account email from section 3.
2. Choose **Forgot your password?**. This is how you set the root password the first time. Follow the email from an `@verify.signin.aws` address and set a **long, unique password** from your password manager.
3. Sign in as root → top-right menu (account name) → **Security credentials** → **Multi-factor authentication (MFA)** → **Assign MFA device**:
   - Name it, e.g. `root-phone`.
   - Choose **Passkey or security key** or **Authenticator app**, and follow the prompts.
   - Add a **second** device if you have one; it saves you if a phone is lost.
4. On the same page, check that **Access keys** shows **none**. Never create access keys for the root user.
5. Save the management account ID (12 digits, top-right menu) in your password manager next to the root credentials, then sign out of root.

### Your Builder ID

This is your everyday sign-in, so it gets MFA too.

1. Go to **https://profile.aws.amazon.com** → **Security** → register a passkey/security key or an authenticator app.
2. Store the Builder ID password and recovery details in your password manager.

Anyone else who needs access is invited as a team member (see [Inviting someone else](#inviting-someone-else)), never given your sign-in.

## 5. Billing guardrails

Do this in the **management account** (AWS Settings → **Projects** → **Management accounts** → **Centrally manage your organization**), before creating anything that costs money. A budget there covers every account in the organization.

1. Search **Billing and Cost Management**.
2. **Budgets** → **Create budget**:
   - Choose **Use a template** → **Monthly cost budget**.
   - Name: `monthly-limit`. Amount: **$20** (the system should cost about $13-15 a month).
   - Email recipients: the root email **and** your own email.
   - **Create budget**. AWS emails you when actual spend passes 85% and 100%, and when forecast spend is heading over.
3. **Billing preferences** → **Alert preferences** → **Edit** → tick **Receive AWS Free Tier alerts**, enter your email, then **Update**.
4. **Account** (top-right menu → **Account**) → **Alternate contacts**: add yourself as the **Billing**, **Operations** and **Security** contact, so AWS notices don't go only to the root mailbox.

## 6. Allow Frankfurt

Activation leaves an organization policy (a service control policy, SCP) whose `RegionFloor` statement denies every region except `us-east-1`, `us-west-2` and the region your project was created in (Stockholm `eu-north-1` for Israel). Until you add Frankfurt to it, creating anything in Frankfurt fails with **AccessDenied**, even though the region selector now offers it.

1. Open the management account console (AWS Settings → **Projects** → **Centrally manage your organization**) and search **AWS Organizations**. AWS Settings may also offer a shortcut: **Manage this organization** → **Manage policies**.
2. **Policies** → **Service control policies** → **AdvancedModeRegionRestrictionSecurityControlPolicy** → **Edit policy**.
3. Scroll to the **last** statement, `"Sid": "RegionFloor"`. Its condition is `StringNotEquals` with a **list** of regions. Add `"eu-central-1"` to that list.
4. **Save changes**.

`RegionFloor` should end up like this (the other regions are whatever AWS put there):

```json
"Condition": {
  "StringNotEquals": {
    "aws:RequestedRegion": [
      "eu-north-1",
      "unspecified",
      "us-east-1",
      "us-west-2",
      "eu-central-1"
    ]
  }
}
```

**Change nothing else.** The two statements above it, `UsEast1Partitional` and `UsWest2Partitional`, also mention regions but do the opposite job: each is a `StringEquals` on a single region (`"us-east-1"`, `"us-west-2"`) that limits that region to global services. Adding Frankfurt there, or turning them into a list, blocks Frankfurt or opens us-east-1 to everything.

**Check it:** in the project account (section 7), switch to Frankfurt, open **Lightsail** and start creating an instance. If you reach the plan picker without an AccessDenied message, it works; cancel without creating anything.

You may leave `eu-north-1` in: nothing will be created there, and the budget would show it if something were. The `BudgetsSpendLimit...` policies in the same list belong to the old spend limit; they should have no targets (open one → **Targets**). If one is still attached to your project, detach it, or it can block new resources.

## 7. Work in the project account, in Frankfurt

1. Sign in at **https://settings.aws.com** with your Builder ID → **Projects** → your project's row → **AWS Console**. Check the account name in the top-right menu before creating anything: it must be the project, not the management account.
2. Top-right region menu → **Europe (Frankfurt) eu-central-1**. The console remembers it.
3. Some services ignore the region, which is expected: **IAM**, **Route 53** (domains and DNS) and **Billing** always show "Global".
4. **Lightsail** has its own region picker inside its console. Choose Frankfurt there too.

Save the project account ID (top-right menu) in your password manager; the runbook's backup bucket and IAM user live in this account.

### Inviting someone else

AWS Settings → **Projects** → the **`<team name>` Identity Delegated Admin** row → **AWS Console** → **IAM Identity Center** → **Users** → **Invite new team member**. Once they accept, **Groups** → the group whose name starts with the project account ID → **Add users to group**. Remove people the same way (AWS Settings → **Projects** → **Actions** → **Manage team**). They sign in with their own Builder ID and MFA.

## 8. Optional: the AWS CLI on your PC

You only need it if you prefer typing to clicking. For example, the runbook's restore section can download a backup either from the S3 console or with the CLI.

1. Install: `winget install -e --id Amazon.AWSCLI` (or the MSI from https://aws.amazon.com/cli/), then open a new terminal and run `aws --version`.
2. Sign in with your Builder ID instead of creating access keys: `aws login --profile drivinglessons`. It opens the browser; choose the project account, and region `eu-central-1` when asked.
3. Test: `aws sts get-caller-identity --profile drivinglessons` prints the **project** account ID.

The session expires after a while; run `aws login --profile drivinglessons` again when commands start failing with expired credentials. Never create long-lived access keys for yourself.

## 9. Check before moving on

- [ ] Advanced features are active: the console shows a region selector.
- [ ] The management root user has a password and MFA, and no access keys. Its email, password and account ID are in the password manager.
- [ ] Your Builder ID has MFA.
- [ ] The `monthly-limit` budget exists in the management account and its emails reach you. Alternate contacts are set.
- [ ] `eu-central-1` is in the `RegionFloor` statement.
- [ ] You can open the project's **AWS Console**, and its region is Frankfurt `eu-central-1`.
- [ ] (Optional) `aws sts get-caller-identity --profile drivinglessons` shows the project account.

## 10. What it will cost

Rough monthly figures (always check the pricing pages; prices change):

| Item | Approximate cost |
|---|---|
| Lightsail 2 GB instance (static IP included while attached) | ~$12/month |
| Route 53 hosted zone | $0.50/month |
| Domain registration | ~$13-15/year for `.com` (varies by TLD) |
| SES email (a few emails a week) | cents |
| S3 backups (30 small dumps) | cents |
| **Total** | **about $13-15/month** plus the yearly domain |

The organization itself, the management account and IAM Identity Center cost nothing.

## 11. Next

Continue with task 8 of the deployment plan, Steps 1-3, **in the project account**: register the domain, start SES verification and the production-access request (these take time, so start them first), then create the Lightsail instance. Things that sometimes catch new accounts:

- **AccessDenied when creating something in Frankfurt.** Frankfurt is missing from `RegionFloor` (section 6), or you're in the management account instead of the project account.
- **Domain registration or the SES production-access request is refused or "pending"** on a brand-new account. Open a case in **Support Center** → **Account and billing** (free with Basic support) and ask for the account to be verified for Route 53 registration / SES production use.
- **Lightsail says the account is not yet activated.** Wait for the activation email from step 2, or check **Billing** → **Payment methods** in the management account to see whether the card was declined.

## Appendix A: Classic account

If you signed up through **Sign up for AWS (advanced)**, or your console already had a region selector, there are no projects, no organization and no Builder ID. Skip sections 3, 6 and 7 and do this instead:

1. **Root user** (instead of section 4): sign in as root → **Security credentials** → assign two MFA devices, check there are no access keys, save the account ID.
2. **Billing** (section 5 as written), plus **Account** → **IAM user and role access to Billing information** → **Edit** → **Activate IAM Access**, so the admin user below can see the bill.
3. **Everyday admin user**: **IAM** → **Users** → **Create user** → `<your-first-name>-admin`, console access with a custom password, **Attach policies directly** → **AdministratorAccess**. Then IAM → **Dashboard** → **Account alias** → `drivinglessons-prod`, so you sign in at `https://drivinglessons-prod.signin.aws.amazon.com/console`. Sign out of root, sign in as the admin user, and assign it MFA under **Security credentials**.
4. **Region**: top-right region menu → **Europe (Frankfurt) eu-central-1**; Lightsail's own picker → Frankfurt.
5. **CLI** (optional): as in section 8, or `aws configure --profile drivinglessons` with an access key for the admin user (IAM → the user → **Security credentials** → **Create access key** → **Command Line Interface (CLI)**). Treat that key like a password and delete it if your PC is lost.
