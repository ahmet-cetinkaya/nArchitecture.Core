# ![NArchitecture Logo](docs/images/n-architecture-logo.png) NArchitecture Core Packages 
![GitHub contributors](https://img.shields.io/github/contributors/kodlamaio-projects/nArchitecture.Core?style=for-the-badge) ![GitHub forks](https://img.shields.io/github/forks/kodlamaio-projects/nArchitecture.Core?style=for-the-badge) ![GitHub stars](https://img.shields.io/github/stars/kodlamaio-projects/nArchitecture.Core?style=for-the-badge) ![GitHub issues](https://img.shields.io/github/issues/kodlamaio-projects/nArchitecture.Core?style=for-the-badge)

## 💻 About The Project

A comprehensive project has been developed that encompasses a wide range of advanced features including Advanced Repository patterns, Dynamic Querying capabilities, JWT (JSON Web Token) authentication, OTP (One-Time Password) implementation, Role-Based Access Management, Distributed Caching using Redis, robust Logging with Serilog, and powerful search functionalities with Elastic Search, among many other features. By contributing to this project, you not only support its growth but also gain valuable experience and knowledge in these cutting-edge technologies.

### Built With

[![](https://img.shields.io/badge/.NET%20Core-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://learn.microsoft.com/tr-tr/dotnet/welcome)

## ⚡ Getting Started

### ⏬ Installation

#### Using NuGet Packages

1. Search for `nArchitecture` on [NuGet](https://www.nuget.org/packages?q=nArchitecture).
2. Install the desired packages in your project.

#### Using Git Submodule

1. Add the submodule to your backend project repository:
   ```bash
   git submodule add https://github.com/kodlamaio-projects/nArchitecture.Core.git src/corePackages
   ```

2. Update the submodule:
   ```bash
   git submodule update --remote src/corePackages
   ```

### 📖 Usage

For detailed usage instructions, please refer to the README of the relevant package.

### Custom Commands
After running `dotnet tool restore`, you can use `dotnet r` to execute and customize commands from global.json.

Example usage:
```bash
dotnet r build
dotnet r test
dotnet r analyze
dotnet r format
```

## 🚧 Roadmap

See the [open issues](https://github.com/kodlamaio-projects/nArchitecture.Core/issues) for a list of proposed features (and known issues).

## 🤝 Contributing

Contributions are what make the open source community such an amazing place to be learn, inspire, and create. Any contributions you make are **greatly appreciated**.

1. Fork the Project
2. Create your Feature Branch (`git checkout -b <feature>/<amazingFeature>'`)
3. Commit your Changes (`git commit -m '<semanticCommitType>(<scope>): <amazingFeature>'`)
   💡 Check [Semantic Commit Messages](./docs/Semantic%20Commit%20Messages.md)
4. Push to the Branch (`git push origin <feature>/<amazingFeature>`)
5. Open a Pull Request

## ⚖️ License

Distributed under the MIT License. See `LICENSE` for more information.

## 📧 Contact

**Project Link:** [https://github.com/kodlamaio-projects/nArchitecture.Core](https://github.com/kodlamaio-projects/nArchitecture.Core)
