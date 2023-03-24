use base64::Engine;
use octorust::auth::Credentials;
use octorust::Client;
use octorust::types::{GitCreateCommitRequest, GitCreateTreeRequest, GitCreateTreeRequestData, GitCreateTreeRequestMode, GitTree, GitUpdateRefRequest, ReposCreateUpdateFileContentsRequest};
use regex::{Captures, Regex, RegexBuilder};
use crate::add_version_to_conandata_yml::AddVersionToConandataYmlArgument;

pub struct CopyGithubFolder<'a> {
    pub github_client: &'a Client,
    pub owner_login: &'a str,
    pub repo_name: &'a str,
    pub branch_name: &'a str,
    pub folder_path: &'a str,
    pub new_folder_path: &'a str,
    pub commit_message: &'a str,
}

pub async fn copy_github_folder(
    argument: &CopyGithubFolder<'_>,
) -> anyhow::Result<()> {
    let CopyGithubFolder {
        repo_name,
        branch_name,
        github_client,
        owner_login,
        folder_path,
        new_folder_path,
        commit_message
    } = *argument;
    let branch = github_client.repos().get_branch(owner_login, repo_name, branch_name).await?;
    let old_tree = github_client.git().get_tree(owner_login, repo_name, &branch_name, "true").await?;

    let new_tree = github_client.git().create_tree(owner_login, repo_name, &GitCreateTreeRequestData {
        base_tree: old_tree.sha,
        tree: old_tree.tree
            .iter()
            .filter(|tree| tree.path.starts_with(&format!("{}/", folder_path)))
            .map(|tree| GitCreateTreeRequest {
                content: "".to_string(),
                mode: Some(
                    serde_json::from_str::<GitCreateTreeRequestMode>(&format!("\"{}\"", &tree.mode)).unwrap()
                ),
                path: String::from(
                    &tree.path.replace(&format!("{}/", folder_path), &format!("{}/", new_folder_path)).to_string()
                ),
                sha: String::from(
                    &tree.sha
                ),
                type_: None,
            })
            .collect(),
    }).await?;
    let new_commit = github_client.git().create_commit(owner_login, repo_name, &GitCreateCommitRequest {
        author: None,
        committer: None,
        message: commit_message.to_string(),
        parents: vec![branch.commit.sha],
        signature: "".to_string(),
        tree: new_tree.sha.to_string(),
    }).await?;
    github_client.git().update_ref(owner_login, repo_name, &format!("heads/{}", branch_name), &GitUpdateRefRequest {
        sha: new_commit.sha,
        force: Some(false),
    }).await?;
    Ok(())
}
