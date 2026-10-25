pub mod publish_check;

use std::path::{Path, PathBuf};

pub type Res<T> = Result<T, String>;

pub fn repo_root() -> PathBuf {
    Path::new(env!("CARGO_MANIFEST_DIR"))
        .parent()
        .expect("xtask has a parent")
        .to_path_buf()
}
