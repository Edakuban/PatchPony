# Supply-chain security

I14.8 makes dependency, container and SBOM controls part of the required GitHub Actions pipeline.

- Pull requests receive GitHub dependency review and fail for newly introduced high/critical vulnerable dependencies or GPL/AGPL licenses.
- Every CI run lists direct and transitive NuGet vulnerabilities in JSON and fails if any are reported.
- Gateway and Worker images are built before Trivy blocks HIGH/CRITICAL OS or library vulnerabilities without a fixed upstream version.
- Syft, via the Anchore SBOM action, creates CycloneDX JSON SBOMs for each tested container image. The SBOMs and NuGet report are retained as private CI artifacts for 30 days.
- Dependabot opens weekly update proposals for NuGet, Docker and GitHub Actions. Updates still pass the complete CI and normal human review; no dependency update is merged automatically.

These controls never upload `.env` files, credentials, source mounts or production images. CI scans only the checkout and locally built `:ci` images. Security findings are remediated through a reviewed change; temporarily ignoring a finding requires a documented, time-bounded exception outside this repository.