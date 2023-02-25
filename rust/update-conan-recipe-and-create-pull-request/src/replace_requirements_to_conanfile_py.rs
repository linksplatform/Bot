use base64::Engine;
use octorust::Client;
use octorust::types::ReposCreateUpdateFileContentsRequest;
use regex::{Captures, RegexBuilder};

use crate::decode_github_content::{decode_github_content, DecodeGithubContentArgument};

pub struct AddRequirementsToConanfilePyArgument<'a> {
    pub github_client: &'a Client,
    pub source_repo_owner_login: &'a String,
    pub source_repo_name: &'a String,
    pub source_repo_branch_name: &'a String,
    pub conanfile_py_file_path: &'a String,
    pub new_version: &'a String,
    pub commit_message: &'a String,
    pub dependencies: &'a Option<Vec<String>>,
}

pub async fn add_requirements_to_conanfile_py(
    argument: &AddRequirementsToConanfilePyArgument<'_>,
) -> anyhow::Result<()> {
    let AddRequirementsToConanfilePyArgument {
        source_repo_name,
        source_repo_branch_name,
        conanfile_py_file_path,
        github_client,
        source_repo_owner_login,
        new_version,
        commit_message, dependencies,
    } = *argument;
    let remote_conanfile_py = github_client
        .repos()
        .get_content_file(
            &source_repo_owner_login,
            &source_repo_name,
            &conanfile_py_file_path,
            &source_repo_branch_name,
        )
        .await
        ?;

    let regex = RegexBuilder::new(r"^(?P<indent> {4})def requirements(\s|.)+?^( {4})(?P<after>\S)")
        .multi_line(true)
        .build()
        ?;
    let decoded_content = decode_github_content(&DecodeGithubContentArgument {
        content: &remote_conanfile_py.content
    }).unwrap();

    let new_content = regex.replace(&decoded_content, |caps: &Captures| {
        let indent = caps.name("indent").unwrap().as_str();
        let after = caps.name("after").unwrap().as_str();
        let dependencies_string = if dependencies.is_none() {
            None
        } else {
            Some(
                dependencies
                    .as_ref()
                    .unwrap()
                    .iter()
                    .map(|dependency| {
                        format!(r#"{indent}{indent}self.requires("{dependency}")"#, indent = indent, dependency = dependency)
                    })
                    .collect::<Vec<_>>()
                    .join("\n")
            )
        };
        format!(
            "{indent}def requirements(self):\n{body}\n\n{indent}{after}",
            indent = indent,
            body = if dependencies_string.is_some() { dependencies_string.unwrap() } else { format!("{indent}{indent}pass", indent = indent) },
            after = after
        )
    });

    github_client
        .repos()
        .create_or_update_file_contents(
            source_repo_owner_login,
            source_repo_name,
            conanfile_py_file_path,
            &ReposCreateUpdateFileContentsRequest {
                author: None,
                sha: remote_conanfile_py.sha,
                message: commit_message.to_string(),
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
