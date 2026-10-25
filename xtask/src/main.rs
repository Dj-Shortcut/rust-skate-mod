use std::process::ExitCode;

use xtask::repo_root;

fn main() -> ExitCode {
    let args: Vec<String> = std::env::args().skip(1).collect();
    match args.first().map(String::as_str) {
        Some("publish-check") => match xtask::publish_check::run_cli(&repo_root()) {
            Ok(()) => ExitCode::SUCCESS,
            Err(error) => {
                println!("{error}");
                ExitCode::FAILURE
            }
        },
        _ => {
            println!("usage: cargo xtask publish-check");
            ExitCode::FAILURE
        }
    }
}
