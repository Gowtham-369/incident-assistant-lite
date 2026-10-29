IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907052048_InitialCreate'
)
BEGIN
    CREATE TABLE [Incidents] (
        [Id] uniqueidentifier NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Severity] nvarchar(20) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [Category] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ResolvedAt] datetime2 NULL,
        [ResolutionNotes] nvarchar(max) NULL,
        CONSTRAINT [PK_Incidents] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907052048_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAt', N'Description', N'ResolutionNotes', N'ResolvedAt', N'Severity', N'Status', N'Title') AND [object_id] = OBJECT_ID(N'[Incidents]'))
        SET IDENTITY_INSERT [Incidents] ON;
    EXEC(N'INSERT INTO [Incidents] ([Id], [Category], [CreatedAt], [Description], [ResolutionNotes], [ResolvedAt], [Severity], [Status], [Title])
    VALUES (''11111111-1111-1111-1111-111111111111'', N''Performance'', ''2026-09-01T14:30:00.0000000Z'', N''p99 latency jumped from 200ms to 4s after the 14:00 deploy. Errors in logs mention connection pool exhaustion.'', NULL, NULL, N''High'', N''Open'', N''API latency spike on checkout service''),
    (''22222222-2222-2222-2222-222222222222'', N''Email'', ''2026-09-03T09:15:00.0000000Z'', N''Password reset emails not being delivered. SMTP relay returning 451 errors intermittently.'', NULL, NULL, N''Medium'', N''InProgress'', N''Users unable to reset passwords''),
    (''33333333-3333-3333-3333-333333333333'', N''Data'', ''2026-08-28T02:00:00.0000000Z'', N''Data warehouse sync failed at step 3 with a timeout. Downstream dashboards showing stale data.'', N''Increased timeout and added retry; root cause was a long-running lock from a manual query.'', ''2026-08-28T08:45:00.0000000Z'', N''Low'', N''Resolved'', N''Nightly ETL job failed'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAt', N'Description', N'ResolutionNotes', N'ResolvedAt', N'Severity', N'Status', N'Title') AND [object_id] = OBJECT_ID(N'[Incidents]'))
        SET IDENTITY_INSERT [Incidents] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907052048_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260907052048_InitialCreate', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024033906_AddRagFields'
)
BEGIN
    ALTER TABLE [Incidents] ADD [AllowedGroups] nvarchar(max) NOT NULL DEFAULT N'["oncall-all"]';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024033906_AddRagFields'
)
BEGIN
    ALTER TABLE [Incidents] ADD [ErrorSignatures] nvarchar(max) NOT NULL DEFAULT N'[]';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024033906_AddRagFields'
)
BEGIN
    ALTER TABLE [Incidents] ADD [RootCause] nvarchar(2000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024033906_AddRagFields'
)
BEGIN
    ALTER TABLE [Incidents] ADD [Service] nvarchar(64) NOT NULL DEFAULT N'unknown';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024033906_AddRagFields'
)
BEGIN
    EXEC(N'UPDATE [Incidents] SET [AllowedGroups] = N''["oncall-all"]'', [ErrorSignatures] = N''[]'', [RootCause] = NULL, [Service] = N''checkout-api''
    WHERE [Id] = ''11111111-1111-1111-1111-111111111111'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024033906_AddRagFields'
)
BEGIN
    EXEC(N'UPDATE [Incidents] SET [AllowedGroups] = N''["oncall-all"]'', [ErrorSignatures] = N''[]'', [RootCause] = NULL, [Service] = N''notification-service''
    WHERE [Id] = ''22222222-2222-2222-2222-222222222222'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024033906_AddRagFields'
)
BEGIN
    EXEC(N'UPDATE [Incidents] SET [AllowedGroups] = N''["oncall-all"]'', [ErrorSignatures] = N''[]'', [RootCause] = NULL, [Service] = N''data-pipeline''
    WHERE [Id] = ''33333333-3333-3333-3333-333333333333'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024033906_AddRagFields'
)
BEGIN
    CREATE INDEX [IX_Incidents_Service] ON [Incidents] ([Service]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024033906_AddRagFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261024033906_AddRagFields', N'10.0.11');
END;

COMMIT;
GO

