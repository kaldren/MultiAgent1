namespace MultiAgent1.Shared.Data;

public static class Seed
{
    public const string InitVisionAgentDatabase = $$"""
    -- Create the database
    CREATE DATABASE vision_agent;

    -- Create the table
    CREATE TABLE vision_agent.dbo.VisionAgent (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Output NVARCHAR(MAX) NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );

    """;
}
