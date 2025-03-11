#!/usr/bin/env bash

mkdir -p dist

# 引数にタグが与えられるため、そのタグを使用してファイル名を変更する
tag=$1

# support platforms, omitted some platforms for now
# see: https://github.com/cli/gh-extension-precompile/blob/561b19deda1228a0edf856c3325df87416f8c9bd/build_and_release.sh#L4-L17
platforms=(
  darwin-amd64
  darwin-arm64
  linux-amd64
  linux-arm64
  windows-amd64
  windows-arm64
)

for platform in "${platforms[@]}"; do
  platform_split=(${platform//-/ })
  os=${platform_split[0]}
  arch=${platform_split[1]}

  output_name="dist/gh-repo-get_${tag}_${os}-${arch}"
  if [ $os = "windows" ]; then
    output_name+=".exe"
  fi

  # https://learn.microsoft.com/ja-jp/dotnet/core/rid-catalog#known-rids
  dotnet_runtime_platform=${os}
  dotnet_runtime_arch=${arch}
  exe_name=""

  case ${os} in
    darwin)
      dotnet_runtime_platform="osx"
      ;;
    linux)
      ;;
    windows)
      dotnet_runtime_platform="win"
      exe_name=".exe"
      ;;
    *)
      echo "Unsupported OS: ${os}"
      exit 1
      ;;
  esac

  if [ $arch = "amd64" ]; then
    dotnet_runtime_arch="x64"
  fi

  dotnet publish -r ${dotnet_runtime_platform}-${dotnet_runtime_arch} -p:DebugSymbols=false
  cp bin/Release/net9.0/${dotnet_runtime_platform}-${dotnet_runtime_arch}/publish/gh-repo-get${exe_name} ${output_name}
done
