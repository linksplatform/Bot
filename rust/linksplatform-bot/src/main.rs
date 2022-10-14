extern crate core;

use std::any::Any;
use std::error::Error;
use std::fs::read_dir;
use octocrab::models;
use walkdir::WalkDir;
use crate::actions::{call_for_each_remote_repository, remove_language_specific_github_workflows_if_folders_do_not_exist, set_https_git_clone_url_instead_of_ssh};
use crate::github_storage_extensions::is_file_exists;

mod actions;
mod github_storage_extensions;

#[tokio::main]
async fn main() -> Result<(), Box<dyn Error>> {
    let github_storage = octocrab::instance();
    // let directory = read_dir(r#"C:\Users\FreePhoenix\Documents\Programming\LinksPlatform"#).unwrap();
    // for directory_entry_result in directory {
    //     let directory_entry = directory_entry_result.unwrap();
    //     if !directory_entry.metadata().unwrap().is_dir() {
    //         continue
    //     }
    //     github_storage
    //         .repos("linksplatform", directory_entry.)
    //
    // }

    call_for_each_remote_repository(github_storage, "linksplatform", |repository| {
        remove_language_specific_github_workflows_if_folders_do_not_exist(github_storage, "linksplatform", repository, "main", vec!["rust", "cpp"])
    })
    Ok(())
}
