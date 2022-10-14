use std::any::{Any, TypeId};
use std::sync::Arc;
use octocrab::models::Repository;
use octocrab::{Error, GitHubError, Octocrab};

pub async fn is_file_exists(github_storage: &Octocrab, repository_owner: &str, repository: &Repository, r#ref: &str, file_path: &str) -> bool {
    if let Err(Error::GitHub {
                   source: GitHubError { message, .. },
                   ..
               }) = github_storage
        .repos(repository_owner, &repository.name)
        .get_content()
        .path(file_path)
        .r#ref(r#ref)
        .send()
        .await {
        message == "Not found"
    } else {
        false
    }
}
