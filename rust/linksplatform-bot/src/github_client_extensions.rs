use std::collections::HashMap;
use std::error::Error;

use async_trait::async_trait;
use octorust::types::{ActionsCreateUpdateRepoSecretRequest, ActionsPublicKey, Order, ReposListOrgSort, ReposListOrgType};

type Result<T> = anyhow::Result<T>;

#[async_trait::async_trait]
pub trait GithubClientActionsExtensions {
    async fn create_or_update_repo_not_encrypted_secret(
        &self,
        owner: &str,
        repo: &str,
        secret_name: &str,
        not_encrypted_body: &str,
        repo_public_key: &ActionsPublicKey,
    ) -> Result<()>;
}

#[async_trait::async_trait]
impl GithubClientActionsExtensions for octorust::actions::Actions {
    async fn create_or_update_repo_not_encrypted_secret(
        &self,
        owner: &str,
        repo: &str,
        secret_name: &str,
        not_encrypted_body: &str,
        repo_public_key: &ActionsPublicKey,
    ) -> Result<()> {
        let encrypted_body = base64::encode(sodiumoxide::crypto::sealedbox::seal(not_encrypted_body.as_bytes(), &sodiumoxide::crypto::box_::PublicKey::from_slice(base64::decode(repo_public_key.key.as_bytes())?.as_slice()).unwrap()));

        self.create_or_update_repo_secret(
            owner,
            repo,
            secret_name,
            &ActionsCreateUpdateRepoSecretRequest {
                key_id: String::from(&repo_public_key.key_id),
                encrypted_value: encrypted_body,
            },
        ).await
    }
}

pub trait GithubClientExtensions {
    async fn update_nuget_tokens_for_every_repository(&self, secret_name: &str, secrets: HashMap<String, String>, owner: &str) -> Result<()>;
}

impl GithubClientExtensions for octorust::Client {
    async fn update_nuget_tokens_for_every_repository(&self, secret_name: &str, secrets: HashMap<String, String>, owner: &str) -> Result<()> {
        let repositories = github.repos().list_all_for_org(owner, ReposListOrgType::Public, ReposListOrgSort::Created, Order::Asc).await?;
        for repository in repositories {
            if !secrets.contains_key(&repository.name) {
                continue;
            }
            let public_key = github.actions().get_repo_public_key(
                owner,
                &repository.name,
            ).await?;

            println!("Creating/updating the {} secret for {}", secret_name, repository.name);

            github.actions().create_or_update_repo_not_encrypted_secret(
                owner,
                &repository.name,
                secret_name,
                secrets.get(&repository.name).unwrap().as_str().unwrap(),
                &public_key,
            ).await?;
        }
        Ok(())
    }
}
