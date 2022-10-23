use std::fs::{File, OpenOptions, read_dir};
use std::io::{Read, Write};
use std::path::Path;
use std::sync::Arc;
use octocrab::models::Repository;
use octocrab::Octocrab;
use regex::Regex;
use walkdir::WalkDir;

pub fn set_https_git_clone_url_instead_of_ssh(repositoryPath: &Path, remoteName: &str) {
    let git_config_path = repositoryPath.join(".git").join("config");
    let mut git_config_file = OpenOptions::new().read(true).open(&git_config_path).expect(&format!("Unable to open the file {}", &git_config_path.to_str().unwrap()));
    let mut contents = String::new();
    git_config_file.read_to_string(&mut contents).expect(&format!("Unable to read the file {}", &git_config_path.to_str().unwrap()));
    let regex = Regex::new(format!(r#"(?P<save>\[remote "{}"\]\s+url = )git@github.com:(?P<ownerAndRepository>.+).git"#, remoteName).as_str()).unwrap();
    let changed_content = regex.replace(&mut contents, "${save} https://github.com/${ownerAndRepository}.git").to_string();
    File::create(&git_config_path).unwrap().write_all(changed_content.as_bytes()).unwrap();
    println!("Changed {} to {} in {}", contents, changed_content, &git_config_path.display());
}

pub async fn call_for_each_remote_repository<F>(github_storage: Arc<Octocrab>,owner: &str, f: F) where F: Fn(Repository) {
    let mut page = github_storage
        .orgs(owner)
        .list_repos()
        .per_page(100)
        .send()
        .await
        .unwrap();
    loop {
        for repository in page.items {
            f(repository);
        }
        page = match github_storage
            .get_page::<Repository>(&page.next)
            .await
            .unwrap()
        {
            Some(next_page) => next_page,
            None => break
        }
    }
}