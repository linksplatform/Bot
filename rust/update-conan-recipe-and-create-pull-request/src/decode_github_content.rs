use {crate::Result, base64::DecodeError};

#[deprecated]
pub struct DecodeGithubContentArgument<'a> {
    pub content: &'a String,
}

#[deprecated(note = "keep `decode_github_content` for soft migration into `decode`")]
pub fn decode_github_content(
    &DecodeGithubContentArgument { content }: &DecodeGithubContentArgument,
) -> Option<String> {
    decode(
        // This leads to unnecessary cloning
        content.clone(),
    )
    .ok()
}

// use `String` because it is useful for `octorust` types
pub fn decode(mut content: String) -> Result<String, DecodeError> {
    // base64 alphabet is always utf8 safe
    Ok(unsafe {
        // `base64::decode` is deprecated but it is very simple use case
        String::from_utf8_unchecked(base64::decode({
            content.retain(|b| !b.is_whitespace());
            content.into_bytes()
        })?)
    })
}
