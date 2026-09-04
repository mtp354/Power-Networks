# Datasets Requiring Accounts or Subscriptions (Saved for Later)

This document tracks datasets and data sources identified from [`ReadMe.txt`](../ReadMe.txt) that cannot be downloaded automatically without registered credentials, academic approval, or commercial subscriptions.

---

## 1. ACLED (Armed Conflict Location & Event Data Project)
- **Website**: [acleddata.com/conflict-data/download-data-files/aggregated-data](https://acleddata.com/conflict-data/download-data-files/aggregated-data)
- **Description**: Weekly aggregated regional conflict, fatality, and civilian exposure events.
- **Why it is saved for later**: All aggregated data downloads and API endpoints are behind authentication (`access_denied` / lock icon).
- **How to activate**:
  1. Register for a free academic/non-commercial account at `https://acleddata.com/user/register`.
  2. Verify email and log in to obtain an API key and user email token.
  3. Set environment variables:
     ```bash
     export ACLED_EMAIL="your_email@institution.edu"
     export ACLED_API_KEY="your_api_key"
     ```
  4. Plug credentials into the ACLED downloader module.

---

## 2. World Elite Database (WED)
- **Website**: [worldelitedatabase.org](https://worldelitedatabase.org/)
- **Description**: Comprehensive standardized cross-country database mapping elite career trajectories, political and economic leadership, and corporate positions.
- **Why it is saved for later**: There is no direct public download link or open API for the raw dataset. The consortium requires researchers to submit a formal request or inquiry.
- **How to activate**:
  1. Contact the research consortium via email at `info@worldelitedatabase.org` specifying the academic institution, project title, and research scope.
  2. Once data access or file drops are granted, place exported files in `data/raw/world_elite_database/`.

---

## 3. OpenCorporates
- **Website**: [opencorporates.com](https://opencorporates.com/)
- **Description**: Global registry of companies, officers, and directors.
- **Why it is saved for later**: Bulk database access and programmatic API calls require an API token. Free individual access is heavily rate-limited and requires registering an account.
- **How to activate**:
  1. Sign up for a free developer or academic research API key at `https://opencorporates.com/users/sign_up`.
  2. Set environment variable:
     ```bash
     export OPENCORPORATES_API_KEY="your_api_key"
     ```

---

## 4. TheGlobalEconomy
- **Website**: [theglobaleconomy.com/download-data.php](https://www.theglobaleconomy.com/download-data.php)
- **Description**: Longitudinal economic and political indicator series across 200+ countries.
- **Why it is saved for later**: Bulk indicator downloads require a paid one-time payment or annual subscription.
- **How to activate**:
  1. Acquire an academic or institutional subscription.
  2. Export desired indicator sets as CSV/XLSX into `data/raw/theglobaleconomy/`.

---

## 5. JetSpy
- **Website**: [jetspy.com](https://jetspy.com/)
- **Description**: Flight tracking and owner/operator intelligence for private jets and corporate aircraft.
- **Why it is saved for later**: Automated access is blocked (Cloudflare 403) and access requires an account / paid subscription.
- **How to activate**:
  1. Create an authorized account at `jetspy.com`.
  2. Export historical flight and registry datasets into `data/raw/jetspy/`.

---

## 6. ImportYeti
- **Website**: [importyeti.com](https://importyeti.com/)
- **Description**: US customs sea shipment records and bill-of-lading data connecting overseas manufacturers, freight intermediaries, and domestic corporate consignees.
- **Why it is saved for later**: Cloudflare protected; bulk exports require a registered account / subscription.
- **How to activate**:
  1. Register for an account at `importyeti.com`.
  2. Download exported supply chain files to `data/raw/importyeti/`.

