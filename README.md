# TSMCMapGenerator

这个项目是一个TSMC（台积电）Map生成工具，旨在帮助用户高效地处理和生成与TSMC Map相关的数据。它通过集成各种服务和数据模型，实现数据的获取、处理和展示。

## 功能

*   **TSMC Map数据处理：** 处理和解析TSMC Map数据。
*   **数据获取：** 从各种来源获取数据，例如STDF文件。
*   **数据模型：** 定义了LotInfoModel, PDataDetail, Stdf_BinsGroupModel等数据模型，用于结构化数据。
*   **服务层：** 提供了TsmcMapService和StdfDataFetcher等服务，用于业务逻辑处理和数据交互。

## 安装

1.  **克隆仓库：**

    ```bash
    git clone https://github.com/wyh1359498723/TSMCMapGenerator.git
    cd TSMCMapGenerator
    ```

2.  **安装.NET SDK：**
    确保您的系统已安装.NET 8.0 SDK。您可以从[Microsoft官网](https://dotnet.microsoft.com/download/dotnet/8.0)下载并安装。

3.  **构建项目：**

    ```bash
    dotnet build
    ```

## 使用方法

项目构建完成后，您可以通过命令行运行它：

```bash
dotnet run
```

具体的命令行参数和配置（例如，STDF文件路径，输出路径等）可能需要根据`Program.cs`或`App.config`中的定义进行调整。请查阅源代码以获取详细信息。

## 项目结构

```
TSMCMapGenerator/
├── App.config               # 应用程序配置文件
├── Models/                  # 数据模型定义
│   ├── LotInfoModel.cs
│   ├── PDataDetail.cs
│   ├── PDataDetailTestInfoModel.cs
│   ├── Stdf_BinsGroupModel.cs
│   ├── Stdf_BinsModel.cs
│   └── StdfApiResponse.cs
├── Services/                # 业务逻辑服务
│   ├── StdfDataFetcher.cs
│   └── TsmcMapService.cs
├── Program.cs               # 程序入口点
├── Repository.cs            # 数据仓储或数据访问层
├── TSMCMapGenerator.csproj  # 项目文件
└── TSMCMapGenerator.sln     # 解决方案文件
```

## 依赖项

本项目主要依赖于 .NET 8.0 框架。具体依赖项请查阅 `TSMCMapGenerator.csproj` 文件。

目前已知的主要依赖包括：

*   **Oracle.ManagedDataAccess.dll:** 用于与 Oracle 数据库交互。
*   **System.Configuration.ConfigurationManager.dll:** 用于访问应用程序配置设置。
*   **System.Diagnostics.EventLog.dll, System.Diagnostics.PerformanceCounter.dll, System.DirectoryServices.Protocols.dll, System.Security.Cryptography.Pkcs.dll, System.Security.Cryptography.ProtectedData.dll:** 这些是.NET框架提供的系统级组件，可能用于日志记录、性能监控、目录服务、加密等。
