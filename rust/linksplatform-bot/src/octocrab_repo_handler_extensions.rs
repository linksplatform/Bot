use std::any::{Any, TypeId};
use std::sync::Arc;
use octocrab::models::Repository;
use octocrab::{Error, GitHubError, Octocrab};
use octocrab::repos::RepoHandler;

#[async_trait::async_trait]
pub trait ExtendedOctocrabRepoHandler {
    async fn is_file_exists(&self, r#ref: &str, file_path: &str) -> bool;
    async fn remove_language_specific_github_workflows_if_folders_do_not_exist(&self, r#ref: &str, languages: Vec<&str>);
}

#[async_trait::async_trait]
impl ExtendedOctocrabRepoHandler for RepoHandler<'_> {
    async fn is_file_exists(&self, r#ref: &str, file_path: &str) -> bool {
        match self
            .get_content()
            .path(file_path)
            .r#ref(r#ref)
            .send()
            .await {
            Err(Error::GitHub {
                    source: GitHubError { message, .. },
                    ..
                }) => {
                message == "Not found"
            }
            _ => {
                false
            }
        }
    }

    async fn remove_language_specific_github_workflows_if_folders_do_not_exist(&self, r#ref: &str, languages: Vec<&str>) {
        for language in languages {
            let language_workflow_path = format!(".github/workflows/{}.yml", language);
            if self.is_file_exists(r#ref, &language_workflow_path) {}
            let a = self
                .get_content()
                .r#ref(r#ref)
                .path(language_workflow_path)
                .send()
                .await?;
            octocrab::instance().repos().file
        }
    }
}



