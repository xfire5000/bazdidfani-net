# Changelog

## 1.4.0

- Added `BranchesAsync()` to list a company's branches with `company_name`, `branch_name`, and `branch_code`.
- Added optional branch filtering to `TechnicalManagersAsync()` and `VehicleAsync()`.
- `SubmitTechnicalInspectionAsync()` checks the selected branch before registration; when `branch_code` is omitted, it checks the parent branch (`1`).
- Fixed repeat registration after a visit is cancelled in the site: a latest visit with status `16` and no SABAF code no longer blocks submission.
