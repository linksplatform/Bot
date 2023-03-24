extern crate core;

use std::error::Error;
use std::iter::zip;
use std::path::Path;

use clap::Parser;
use octorust::{auth::Credentials, Client};
use octorust::types::{GitCreateBlobRequest, GitCreateCommitRequest, GitCreateTreeRequest, GitCreateTreeRequestData, GitCreateTreeRequestMode, GitUpdateRefRequest, PullsCreateRequest, ReposCreateUpdateFileContentsRequest};

use crate::add_version_to_conandata_yml::{add_version_to_conandata_yml, AddVersionToConandataYmlArgument};
use crate::add_version_to_config_yml::{add_version_to_config_yml, AddVersionToConfigYmlArgument};
use crate::copy_github_folder::{copy_github_folder, CopyGithubFolder};
use crate::replace_requirements_to_conanfile_py::{add_requirements_to_conanfile_py, AddRequirementsToConanfilePyArgument};

mod replace_requirements_to_conanfile_py;
mod add_version_to_conandata_yml;
mod add_version_to_config_yml;
mod copy_github_folder;
mod decode_github_content;

/// Simple program to greet a person
#[derive(Parser, Debug)]
#[command(author, version, about, long_about = None)]
struct Args {
    /// Github authentication token
    #[arg(long)]
    github_authentication_token: String,

    /// New version
    #[arg(long)]
    new_version: String,

    /// Recipe name
    #[arg(long)]
    recipe_name: String,

    /// Source repo source_repo_owner_login
    #[arg(long)]
    source_repo_owner_login: String,

    /// Source repo name
    #[arg(long)]
    source_repo_name: String,

    /// Source repo reference
    #[arg(long)]
    source_repo_branch_name: String,

    /// destination repo source_repo_owner_login
    #[arg(long)]
    destination_repo_owner_login: String,

    /// destination repo name
    #[arg(long)]
    destination_repo_name: String,

    /// destination repo branch
    #[arg(long)]
    destination_repo_branch_name: String,

    /// Url of zip with a library
    #[arg(long)]
    lib_zip_url: String,

    /// SHA256 hash of zip which contains a library
    #[arg(long)]
    sha256hash: String,

    /// Dependencies in library_name@version format
    #[clap(long, num_args = 0.., value_delimiter = ' ')]
    dependencies: Vec<String>,

    /// Title of a pull request that will be created
    #[arg(long)]
    pull_request_title: Option<String>,

    /// Body of a pull request that will be created
    #[arg(long)]
    pull_request_body: Option<String>,

    /// Previous version
    #[arg(long)]
    previous_version: String,
}

#[tokio::main]
async fn main() -> Result<(), Box<dyn Error>> {
    let Args {
        github_authentication_token, new_version, recipe_name, source_repo_owner_login, source_repo_name, source_repo_branch_name, destination_repo_owner_login, destination_repo_name, destination_repo_branch_name, lib_zip_url, sha256hash, dependencies, pull_request_title, pull_request_body, previous_version
    } = Args::parse();

    let github_client = Client::new(
        String::from("user-agent-name"),
        Credentials::Token(github_authentication_token),
    ).unwrap();

    copy_github_folder(&CopyGithubFolder {
        github_client: &github_client,
        owner_login: &source_repo_owner_login,
        repo_name: &source_repo_name,
        branch_name: &source_repo_branch_name,
        new_folder_path:  &format!("recipes/{}/{}", recipe_name, new_version),
        folder_path: &format!("recipes/{}/{}", recipe_name, previous_version),
        commit_message: &format!("Copy {} version folder for {} version", previous_version, new_version),
    }).await.unwrap();
    
    add_version_to_config_yml(&AddVersionToConfigYmlArgument {
        github_client: &github_client,
        source_repo_owner_login: &source_repo_owner_login,
        source_repo_name: &source_repo_name,
        source_repo_branch_name: &source_repo_branch_name,
        config_yml_file_path: &format!("recipes/{recipe_name}/config.yml"),
        new_version: &new_version,
    });

    add_version_to_conandata_yml(&AddVersionToConandataYmlArgument {
        github_client: &github_client,
        owner_login: &source_repo_owner_login,
        repo_name: &source_repo_name,
        branch_name: &source_repo_branch_name,
        file_path: &format!("recipes/{recipe_name}/{new_version}/conandata.yml"),
        new_version: &new_version,
        sha256hash: &sha256hash,
        lib_zip_url: &lib_zip_url,
        commit_message: &format!("Add {} version", new_version),
    }).await.unwrap();

    add_requirements_to_conanfile_py(&AddRequirementsToConanfilePyArgument {
        github_client: &github_client,
        source_repo_owner_login: &source_repo_owner_login,
        source_repo_name: &source_repo_name,
        source_repo_branch_name: &source_repo_branch_name,
        conanfile_py_file_path: &format!("recipes/{recipe_name}/{new_version}/conanfile.py"),
        new_version: &new_version,
        commit_message: &"Add requirements".to_string(),
        dependencies: &Some(dependencies),
    }).await.unwrap();

    github_client.pulls().create(
        &destination_repo_owner_login,
        &destination_repo_name,
        &PullsCreateRequest {
            base: destination_repo_branch_name.to_string(),
            body: pull_request_body.unwrap_or(String::from("")).to_string(),
            draft: None,
            head: format!("{owner_login}:{branch_name}", owner_login = source_repo_owner_login, branch_name = source_repo_branch_name),
            issue: 0,
            maintainer_can_modify: Some(true),
            title: pull_request_title.unwrap_or(format!("[{recipe_name}] Add {new_version} version ")),
        }
    ).await.unwrap();

    Ok(())
}
