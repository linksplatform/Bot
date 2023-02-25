use base64::Engine;
use octorust::Client;
use octorust::types::ReposCreateUpdateFileContentsRequest;
use regex::{Captures, Regex, RegexBuilder};
use serde_json::to_string;
use crate::decode_github_content::{decode_github_content, DecodeGithubContentArgument};

pub struct AddVersionToConfigYmlArgument<'a> {
    pub github_client: &'a Client,
    pub source_repo_owner_login: &'a String,
    pub source_repo_name: &'a String,
    pub source_repo_branch_name: &'a String,
    pub config_yml_file_path: &'a String,
    pub new_version: &'a String,
}

pub async fn add_version_to_config_yml(
    argument: &AddVersionToConfigYmlArgument<'_>,
) -> anyhow::Result<()> {
    let AddVersionToConfigYmlArgument {
        source_repo_name,
        source_repo_branch_name,
        github_client,
        source_repo_owner_login,
        config_yml_file_path, new_version,
    } = argument;
    let remote_config_yml = github_client
        .repos()
        .get_content_file(
            &source_repo_owner_login,
            &source_repo_name,
            &config_yml_file_path,
            &source_repo_branch_name,
        )
        .await
        ?;
    let decoded_content = decode_github_content(&DecodeGithubContentArgument {
        content: &remote_config_yml.content
    }).unwrap();
    let new_content = format!(r#"{old_content}\n  "{new_version}":\n    folder: {new_version}"#, old_content = decoded_content, new_version = new_version);
    github_client
        .repos()
        .create_or_update_file_contents(
            source_repo_owner_login,
            source_repo_name,
            config_yml_file_path,
            &ReposCreateUpdateFileContentsRequest {
                author: None,
                sha: remote_config_yml.sha,
                message: String::from("Add new version"),
                branch: source_repo_branch_name.to_string(),
                committer: None,
                content: base64::engine::general_purpose::STANDARD.encode(
                    new_content.to_string()
                ),
            },
        )
        .await
        ?;
    Ok(())
}
