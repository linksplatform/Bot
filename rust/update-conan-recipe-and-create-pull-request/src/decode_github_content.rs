use base64::Engine;
use crate::add_version_to_conandata_yml::AddVersionToConandataYmlArgument;

pub struct DecodeGithubContentArgument<'a> {
    pub content: &'a String,
}

pub fn decode_github_content(argument: &DecodeGithubContentArgument<'_>) -> Option<String> {
    let DecodeGithubContentArgument {
        content
    } = argument;
    let mut content_without_github_shit = content.as_bytes().to_owned();
    content_without_github_shit.retain(|b| !b" \n\t\r\x0b\x0c".contains(b));
    let decoded_content = base64::engine::general_purpose::STANDARD
        .decode(&content_without_github_shit).unwrap();
    Some(String::from_utf8_lossy(&decoded_content).into_owned())
}