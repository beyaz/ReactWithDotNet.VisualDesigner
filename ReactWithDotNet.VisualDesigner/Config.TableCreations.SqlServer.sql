BEGIN TRANSACTION;
  
BEGIN TRY
  
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'RVD')
    EXEC('CREATE SCHEMA RVD');
  
      
    CREATE TABLE RVD.Project (
        Id           INT IDENTITY(1,1) PRIMARY KEY,
        Name         VARCHAR(500),
        ConfigAsYaml NVARCHAR(MAX)
    );
      
    CREATE TABLE RVD.Component (
        Id                INT IDENTITY(1,1) PRIMARY KEY,
        ProjectId         INT NOT NULL,
        RootElementAsYaml NVARCHAR(MAX) NULL,
        ConfigAsYaml      NVARCHAR(MAX) NULL,
        CONSTRAINT FK_Component_Project FOREIGN KEY (ProjectId) REFERENCES RVD.Project(Id)
    );
      
    CREATE TABLE RVD.[User] (
        Id                 INT IDENTITY(1,1) PRIMARY KEY,
        UserName           NVARCHAR(255) NOT NULL,
        ProjectId          INT NOT NULL,
        LastAccessTime     DATETIME NOT NULL,
        LastStateAsYaml    NVARCHAR(MAX) NULL,
        LocalWorkspacePath NVARCHAR(1000) NULL
    );
    
    CREATE TABLE RVD.ComponentWorkspace (
        Id                INT IDENTITY(1,1) PRIMARY KEY,
        ComponentId       INT NOT NULL,
        RootElementAsYaml NVARCHAR(MAX) NULL,
        UserName          NVARCHAR(255) NULL,
        LastAccessTime    DATETIME NOT NULL,
        CONSTRAINT FK_ComponentWorkspace_Component FOREIGN KEY (ComponentId) REFERENCES RVD.Component(Id)
    );
    
    CREATE TABLE RVD.ComponentHistory (
        Id                      INT IDENTITY(1,1) PRIMARY KEY,
        ComponentId             NUMERIC(18,0) NOT NULL,
        ComponentRootElementAsYaml NVARCHAR(MAX) NULL,
        UserName                NVARCHAR(255) NULL,
        InsertTime              DATETIME NOT NULL,
        ConfigAsYaml            NVARCHAR(MAX) NULL
    );

COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    THROW;
END CATCH; 