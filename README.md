# TaskDB

Aplicación de tareas con C#, Windows Forms, .NET Framework 4.8 y SQL Server LocalDB.

## Preparación

1. Instalar Visual Studio con la carga **Desarrollo de escritorio de .NET**, el SDK de .NET Framework 4.8 y **SQL Server Express LocalDB**.
2. Comprobar la instancia con `SqlLocalDB info MSSQLLocalDB`. Si no existe, crearla con `SqlLocalDB create MSSQLLocalDB`.
3. Abrir `TaskDB1.sln`, compilar y ejecutar.

La aplicación crea automáticamente la base y la tabla al arrancar. Los datos se guardan en `%LOCALAPPDATA%\TaskDB1\TaskDB.mdf` y su archivo de log. LocalDB usa la versión instalada en cada equipo; no se copian bases binarias desde Git. El catálogo tiene un sufijo calculado a partir de la carpeta para evitar colisiones. La conexión está en `TaskDB1/App.config` y usa `|DataDirectory|`.

`Database/schema.sql` es la definición compartida y se copia al directorio de salida durante la compilación. La inicialización se puede ejecutar varias veces sin borrar las tareas.

Los antiguos archivos `.mdf` y `.ldf` del proyecto se conservan en el equipo, pero dejan de estar versionados y no se usan para la nueva base. No se migran sus registros automáticamente.

## Compilación desde una consola de Visual Studio

```powershell
MSBuild.exe TaskDB1.sln /t:Build /p:Configuration=Debug
```

La solución necesita el MSBuild de Visual Studio para las tareas de recursos de .NET Framework.
