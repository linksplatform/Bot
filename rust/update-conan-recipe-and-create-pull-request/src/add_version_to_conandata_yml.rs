use anyhow::anyhow;
use base64::Engine;
use octorust::Client;
use octorust::types::{GitTree, ReposCreateUpdateFileContentsRequest};
use regex::{Captures, Regex, RegexBuilder};
use crate::decode_github_content::{decode_github_content, DecodeGithubContentArgument};

pub struct AddVersionToConandataYmlArgument<'a> {
    pub github_client: &'a Client,
    pub owner_login: &'a String,
    pub repo_name: &'a String,
    pub branch_name: &'a String,
    pub file_path: &'a String,
    pub new_version: &'a String,
    pub sha256hash: &'a String,
    pub lib_zip_url: &'a String,
    pub commit_message: &'a String,
}

pub async fn add_version_to_conandata_yml(
    argument: &AddVersionToConandataYmlArgument<'_>,
) -> anyhow::Result<()> {
    let AddVersionToConandataYmlArgument {
        repo_name,
        branch_name,
        file_path,
        github_client,
        owner_login,
        new_version,
        sha256hash,
        lib_zip_url,
        commit_message
    } = *argument;
    let remote_conandata_yml = github_client
        .repos()
        .get_content_file(
            &owner_login,
            &repo_name,
            &file_path,
            &branch_name,
        )
        .await
        ?;

    let decoded_content = decode_github_content(&DecodeGithubContentArgument {
        content: &remote_conandata_yml.content
    }).unwrap();

    let new_content = format!("{old_content}  {new_version}:\n    url: {url}\n    sha256: {sha256}\n", old_content = decoded_content, new_version = new_version, url = lib_zip_url, sha256 = sha256hash);


    github_client
        .repos()
        .create_or_update_file_contents(
            owner_login,
            repo_name,
            file_path,
            &ReposCreateUpdateFileContentsRequest {
                author: None,
                sha: remote_conandata_yml.sha,
                message: commit_message.to_string(),
                branch: branch_name.to_string(),
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
