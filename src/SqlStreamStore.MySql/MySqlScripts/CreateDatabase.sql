/* MySQL 5.6+ */

DELIMITER $$

CREATE PROCEDURE `CreateIndex`
    (
        IN given_table    VARCHAR(64),
        IN given_index    VARCHAR(64),
        IN given_columns  VARCHAR(64)
    )
    BEGIN

        DECLARE IndexIsThere INTEGER;
        DECLARE sqlstmt VARCHAR(256);

        SELECT COUNT(1) INTO IndexIsThere
        FROM INFORMATION_SCHEMA.STATISTICS
        WHERE table_schema = DATABASE()
              AND   table_name   = given_table
              AND   index_name   = given_index;

        IF IndexIsThere = 0 THEN
            SET sqlstmt = CONCAT('CREATE UNIQUE INDEX ',given_index,' ON ',
                                  given_database,'.',given_table,' (',given_columns,')');
            PREPARE st FROM sqlstmt;
            EXECUTE st;
            DEALLOCATE PREPARE st;
        END IF;

    END $$

DELIMITER ;

CREATE TABLE IF NOT EXISTS Streams (
    Id                  CHAR(42)                                NOT NULL,
    IdInternal          INT                                     NOT NULL,
    IdOriginal          NVARCHAR(1000)                          NOT NULL,
    Version             INT                 DEFAULT -1          NOT NULL,
    Position            BIGINT              DEFAULT -1          NOT NULL,
    PRIMARY KEY(IdInternal)
);

CALL CreateIndex(
    'Streams',
    'IX_Streams_Id',
    'Id'
)

CREATE TABLE Messages IF NOT EXISTS(
    Position            BIGINT                                  NOT NULL,
    StreamIdInternal    INT                                     NOT NULL,
    StreamVersion       INT                                     NOT NULL,
    Id                  BINARY(16)                              NOT NULL,
    Created             DATETIME                                NOT NULL,
    Type                NVARCHAR(128)                           NOT NULL,
    JsonData            NVARCHAR(max)                           NOT NULL,
    JsonMetadata        NVARCHAR(max)                                   ,
    PRIMARY KEY(Position),
    FOREIGN KEY (StreamIdInternal) REFERENCES Streams(IdInternal)
);

BEGIN
    IF (
      SELECT COUNT (1)
      FROM INFORMATION_SCHEMA.STATISTICS
      WHERE table_schema = DATABASE ()
      AND TABLE_NAME = 'Messages'
      AND index_name   = 'IX_Messages_Position'  
    ) = 0 THEN
        CREATE UNIQUE INDEX IX_Messages_Position
        ON                  Messages (Position);
    END IF
END

BEGIN
    IF (
        SELECT COUNT (1)
        FROM INFORMATION_SCHEMA.STATISTICS
        WHERE table_schema = DATABASE ()
        AND TABLE_NAME = 'Messages'
        AND index_name   = 'IX_Messages_StreamIdInternal_Id'
    ) = 0 THEN
        CREATE UNIQUE INDEX IX_Messages_StreamIdInternal_Id
        ON                  Messages (StreamIdInternal, Id);
    END IF
END

BEGIN
    IF (
        SELECT COUNT(1)
        FROM   INFORMATION_SCHEMA.STATISTICS
        WHERE  table_schema = DATABASE()
        AND    table_name   = 'Messages'
        AND    index_name   = 'IX_Messages_StreamIdInternal_Revision'
    ) = 0 THEN
        CREATE UNIQUE INDEX IX_Messages_StreamIdInternal_Revision
        ON                  Messages (StreamIdInternal, StreamVersion);
    END IF
END

BEGIN
    IF (
        SELECT COUNT(1)
        FROM   INFORMATION_SCHEMA.STATISTICS
        WHERE  table_schema = DATABASE()
        AND    table_name   = 'Messages'
        AND    index_name   = 'IX_Messages_StreamIdInternal_Created'
    ) = 0 THEN
        CREATE UNIQUE INDEX IX_Messages_StreamIdInternal_Created
        ON              Messages (StreamIdInternal, Created)
    END IF
END