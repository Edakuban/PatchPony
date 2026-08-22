\# overview

.codecell/

├── project.yaml

├── skills/

│   ├── architecture.md

│   ├── backend.md

│   ├── frontend.md

│   ├── database.md

│   └── testing.md

└── policy.yaml



\#sample

name: billing-module



paths:

&#x20; - src/billing/\*\*

&#x20; - config/billing.yaml



description: |

&#x20; Handles invoices and payments.



rules:

&#x20; - Never modify generated files.

&#x20; - Database migrations live in migrations/.

&#x20; - Run billing tests after changes.



tests:

&#x20; - pytest tests/billing

