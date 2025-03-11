# gh-repo-get

gh-repo-getは[ghq](https://github.com/x-motemen/ghq)にインスパイアされたGitHub CLI拡張機能です。`repo-get.root`で指定されたディレクトリをルートとして、リポジトリのクローンを管理します。

## インストール方法

```bash
gh extension install yamachu/gh-repo-get
```

## 使用方法

```bash
# リポジトリをクローン
gh repo-get https://github.com/user/repo

# リポジトリ名からクローン（GitHub）
gh repo-get user/repo
```

### 使用例

```bash
# GitHubからリポジトリをクローン
gh repo-get octocat/hello-world

# GitHubからgh authでログイン済みのユーザのリポジトリをクローン
gh repo-get hello-world # user/hello-worldとして扱われる

# 完全URLからリポジトリをクローン
gh repo-get https://github.com/user/project
```

## 設定

リポジトリがクローンされるルートディレクトリを設定します：

```bash
# ルートディレクトリを設定
gh config set repo-get.root ~/repos

# 現在のルートディレクトリ設定を確認
gh config get repo-get.root
```

デフォルトでは、リポジトリは以下の構造でクローンされます：

```
~/repos
└── github.com
    ├── user1
    │   └── repo1
    └── user2
        └── repo2
```

## ライセンス

MIT
