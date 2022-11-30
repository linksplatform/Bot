extern crate core;

use std::collections::HashMap;
use std::env;
use std::error::Error;

use octorust::auth::Credentials;
use octorust::Client;
use octorust::types::{ActionsCreateUpdateRepoSecretRequest, Order, ReposListOrgSort, ReposListOrgType};
use serde_json::{json, Value};

use crate::github_client_extensions::GithubClientActionsExtensions;

mod github_client_extensions;

#[tokio::main]
async fn main() -> Result<(), Box<dyn Error>> {
    let github_client = Client::new(
        String::from("LinksPlatformBot"),
        Credentials::Token(
            env::var("GITHUB_TOKEN")?
        ),
    ).unwrap();

    Ok(())
}

