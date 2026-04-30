# 📝 Online Examination Management System (OEMS)

A full-stack web application built with **ASP.NET Core 8 MVC** that digitizes the entire examination workflow — from scheduling and question management to automated evaluation and result delivery.

---

## 🚀 Live Demo

> _Coming soon / hosted locally_

---

## 📌 Features

### 👨‍💼 Admin
- Manage Staff and Student accounts
- Oversee all exams, subjects, and results
- Full system control via dashboard

### 👩‍🏫 Staff
- Create and schedule exams
- Upload question papers individually or via **Excel bulk upload (100+ questions at once)**
- View student results and performance

### 🎓 Student
- Register and log in securely
- Attempt scheduled exams within allowed time
- Receive **automated email notifications** for exam schedules and results
- Auto-logged out immediately after exam submission (prevents re-entry)

---

## ⚙️ Tech Stack

| Layer | Technology |
|---|---|
| **Frontend** | Razor Pages, JavaScript, jQuery, HTML/CSS |
| **Backend** | ASP.NET Core 8 MVC, C# |
| **Database** | MS SQL Server 2022 |
| **ORM** | Dapper with Stored Procedures |
| **Architecture** | 3-Tier (Presentation → Business Logic → Data Access) |
| **Auth** | Cookie-based, Claim-based Role Authentication |
| **Tools** | Visual Studio, Git, MS Excel |

---

## 🏗️ Architecture Overview

```
┌─────────────────────────────────┐
│        Presentation Layer       │  ← Razor Pages, MVC Controllers
├─────────────────────────────────┤
│       Business Logic Layer      │  ← C# Services, Validation, Rules
├─────────────────────────────────┤
│        Data Access Layer        │  ← Dapper ORM + Stored Procedures
├─────────────────────────────────┤
│         MS SQL Server 2022      │  ← Normalized Database
└─────────────────────────────────┘
```

---

## 🗄️ Database Design

- Normalized relational schema across Users, Roles, Exams, Subjects, Questions, Results
- All data operations handled via **Stored Procedures** for performance and security
- Role-based data access enforced at both application and database level

---

## 🔐 Authentication & Authorization

- **Cookie-based authentication** with **Claim-based authorization**
- Each role (Admin / Staff / Student) has scoped access — unauthorized routes redirect automatically
- Session is **invalidated server-side** immediately on exam submission

---

## 📧 Email Notifications

Automated emails are triggered for:
- Exam schedule announcements
- Result publication alerts

---

## 🛠️ How to Run Locally

### Prerequisites
- [Visual Studio 2022+](https://visualstudio.microsoft.com/)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [MS SQL Server 2022](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
- [SQL Server Management Studio (SSMS)](https://learn.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms)

### Steps

**1. Clone the repository**
```bash
git clone https://github.com/abuostar101-glitch/OEMS.git
cd OEMS
```

**2. Set up the database**
- Open SSMS and connect to your local SQL Server instance
- Run the SQL script located at `/Database/OEMS_Setup.sql` to create the database and all tables/stored procedures

**3. Configure the connection string**
- Open `appsettings.json`
- Update the connection string with your SQL Server instance name:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=OEMSDB;Trusted_Connection=True;"
}
```

**4. Configure email settings (optional)**
- In `appsettings.json`, add your SMTP credentials:
```json
"EmailSettings": {
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": 587,
  "SenderEmail": "your-email@gmail.com",
  "SenderPassword": "your-app-password"
}
```

**5. Run the application**
- Open the solution in Visual Studio
- Press `F5` or click **Run**
- The app opens at `https://localhost:xxxx`

### Default Login Credentials
| Role | Username | Password |
|---|---|---|
| Admin | admin@oems.com | Admin@123 |
| Staff | staff@oems.com | Staff@123 |
| Student | student@oems.com | Student@123 |

> ⚠️ Change these credentials after first login.

---

## 📂 Project Structure

```
OEMS/
├── Controllers/          # MVC Controllers for each role
├── Models/               # Data models and view models
├── Views/                # Razor Pages (.cshtml)
├── Services/             # Business logic layer
├── DataAccess/           # Dapper repositories + stored procedures
├── Database/             # SQL scripts for setup
├── wwwroot/              # Static files (CSS, JS, images)
└── appsettings.json      # Configuration
```

---

## 🙋‍♂️ Author

**Abu Bakar A**
- 📧 abuostar101@gmail.com
- 🔗 [GitHub](https://github.com/abuostar101-glitch)
- 📍 Chennai, Tamil Nadu

---

## 📄 License

This project is open source and available under the [MIT License](LICENSE).
