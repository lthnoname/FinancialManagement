CREATE DATABASE PersonalFinanceDB;
GO

USE PersonalFinanceDB;
GO


-- =============================================
-- 1. USERS
-- =============================================

CREATE TABLE Users
(
    UserId INT IDENTITY(1,1) PRIMARY KEY,

    Username NVARCHAR(50) NOT NULL,
    Email NVARCHAR(100) NOT NULL,

    PasswordHash NVARCHAR(255) NOT NULL,

    FullName NVARCHAR(100) NULL,

    CreatedAt DATETIME2 NOT NULL
        DEFAULT SYSDATETIME(),

    IsActive BIT NOT NULL
        DEFAULT 1,

    CONSTRAINT UQ_Users_Username UNIQUE (Username),
    CONSTRAINT UQ_Users_Email UNIQUE (Email)
);
GO


-- =============================================
-- 2. CATEGORIES
-- Loại thu / loại chi của từng user
-- =============================================

CREATE TABLE Categories
(
    CategoryId INT IDENTITY(1,1) PRIMARY KEY,

    UserId INT NOT NULL,

    CategoryName NVARCHAR(100) NOT NULL,

    -- I = Income (Thu)
    -- E = Expense (Chi)
    Type CHAR(1) NOT NULL,

    CreatedAt DATETIME2 NOT NULL
        DEFAULT SYSDATETIME(),

    CONSTRAINT CK_Categories_Type
        CHECK (Type IN ('I', 'E')),

    CONSTRAINT FK_Categories_Users
        FOREIGN KEY (UserId)
        REFERENCES Users(UserId)
        ON DELETE CASCADE,

    CONSTRAINT UQ_Categories_User_Name_Type
        UNIQUE (UserId, CategoryName, Type),

    -- Dùng để đảm bảo Transaction chỉ sử dụng
    -- category của chính user đó
    CONSTRAINT UQ_Categories_User_Category_Type
        UNIQUE (UserId, CategoryId, Type)
);
GO


-- =============================================
-- 3. TRANSACTIONS
-- Các khoản thu / chi
-- =============================================

CREATE TABLE [Transactions]
(
    TransactionId INT IDENTITY(1,1) PRIMARY KEY,

    UserId INT NOT NULL,

    CategoryId INT NOT NULL,

    -- I = Income
    -- E = Expense
    Type CHAR(1) NOT NULL,

    Amount DECIMAL(18,2) NOT NULL,

    TransactionDate DATETIME2 NOT NULL
        DEFAULT SYSDATETIME(),

    Note NVARCHAR(500) NULL,

    CreatedAt DATETIME2 NOT NULL
        DEFAULT SYSDATETIME(),

    CONSTRAINT CK_Transactions_Type
        CHECK (Type IN ('I', 'E')),

    CONSTRAINT CK_Transactions_Amount
        CHECK (Amount > 0),

    CONSTRAINT FK_Transactions_Users
        FOREIGN KEY (UserId)
        REFERENCES Users(UserId)
        ON DELETE CASCADE,

    CONSTRAINT FK_Transactions_Categories
        FOREIGN KEY (UserId, CategoryId, Type)
        REFERENCES Categories(UserId, CategoryId, Type)
);
GO