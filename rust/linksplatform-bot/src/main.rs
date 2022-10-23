extern crate core;

use std::any::Any;
use std::error::Error;
use std::fs::read_dir;
use octocrab::Error::Json;
use octocrab::models;
use octocrab::models::User;
use serde_json::json;
use walkdir::WalkDir;
use crate::actions::{call_for_each_remote_repository, set_https_git_clone_url_instead_of_ssh};

mod actions;
mod octocrab_repo_handler_extensions;

#[tokio::main]
async fn main() -> Result<(), Box<dyn Error>> {
    let github_storage = octocrab::instance();
    let repo_handler = github_storage.repos("linksplatform", "Bot");
    let a = repo_handler.get_content();
    println!("{}", a.path("LICENSE").send().await.unwrap().items[0].name);
    println!("{}", a.path("LICENSE").send().await.unwrap().items[0].name);
    // let result: User = github_storage.get("/repos/{owner}/{repo}/git/trees/{tree_sha}", Some(&json!({
    //     "owner": "linksplatform",
    //     "repo": "Bot",
    //     "tree_sha": "7d9bf076f039ff21652701cb6ae3b61a97c3cc6b"
    // })));
    // result.
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

    // call_for_each_remote_repository(github_storage, "linksplatform", |repository| {
    //     remove_language_specific_github_workflows_if_folders_do_not_exist(github_storage, "linksplatform", repository, "main", vec!["rust", "cpp"])
    // })
    Ok(())
}
