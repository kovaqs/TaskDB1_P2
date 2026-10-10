IF OBJECT_ID(N'dbo.Tareas', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tareas
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Tareas PRIMARY KEY,
        Titulo nvarchar(200) NOT NULL,
        Descripcion nvarchar(2000) NOT NULL CONSTRAINT DF_Tareas_Descripcion DEFAULT N'',
        Estado nvarchar(10) NOT NULL CONSTRAINT DF_Tareas_Estado DEFAULT N'Pendiente',
        FechaCreacion datetime2(0) NOT NULL CONSTRAINT DF_Tareas_FechaCreacion DEFAULT SYSDATETIME(),
        CONSTRAINT CK_Tareas_Titulo CHECK (LEN(LTRIM(RTRIM(Titulo))) > 0),
        CONSTRAINT CK_Tareas_Estado CHECK (Estado IN (N'Pendiente', N'Completada'))
    );
END;

IF COL_LENGTH(N'dbo.Tareas', N'Id') IS NULL
    OR COL_LENGTH(N'dbo.Tareas', N'Titulo') IS NULL
    OR COL_LENGTH(N'dbo.Tareas', N'Descripcion') IS NULL
    OR COL_LENGTH(N'dbo.Tareas', N'Estado') IS NULL
    OR COL_LENGTH(N'dbo.Tareas', N'FechaCreacion') IS NULL
    THROW 50001, 'La tabla Tareas no coincide con Database/schema.sql. Revisa el esquema de la base local.', 1;
