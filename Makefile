# Repo checks for the Rust skate mod. The mod itself (mods/rust) is C# and is
# built with its own tools; see mods/rust/client/README.md and
# mods/rust/plugins/README.md.

.PHONY: help publish-check assets check

help:
	@echo "make publish-check   what a push would publish, read before it happens"
	@echo "make assets          authored model files match assets/authored/manifest.json"
	@echo "make check           both"

publish-check:
	cargo xtask publish-check

assets:
	python3 scripts/check_assets.py

check: publish-check assets
