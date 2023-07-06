import {Octokit} from "octokit";

const octokit = new Octokit({auth: process.env.auth});



const main = async () => {
    const {data: {login}} = await octokit.rest.users.getAuthenticated();

    let result = await octokit.request('GET /repos/{owner}/{repo}/git/trees/{tree_sha}', {
        owner: 'linksplatform',
        repo: 'Bot',
        tree_sha: '7d9bf076f039ff21652701cb6ae3b61a97c3cc6b'
    })

    console.log({result});
};



main();