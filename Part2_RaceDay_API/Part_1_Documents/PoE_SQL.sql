USE PoE_SQL;



/* ================================
   CREATE EVENT ORGANISER TABLE
   ================================ */

CREATE TABLE EVENT_ORGANISER (
    OrganiserId INT IDENTITY (1,1) UNIQUE,
    OrganiserName VARCHAR(200) NOT NULL,
    Email VARCHAR(200) NOT NULL UNIQUE,
    Phone VARCHAR(20) NOT NULL UNIQUE
);


/* ================================
   INSERT EVENT ORGANISERS
   ================================ */

INSERT EVENT_ORGANISER (OrganiserName, Email, Phone)
VALUES
('Comrades Marathon Association', 'info@comrades.co.za', '+27334212210'),
('Cape Town Cycle Events', 'info@capetowncycle.co.za', '+27214659000');


/* ================================
   CREATE PARTICIPANT TABLE
   ================================ */

CREATE TABLE PARTICIPANT (
    ParticipantId INT IDENTITY (1,1) UNIQUE,
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    Email VARCHAR(200) NOT NULL UNIQUE,
    Phone VARCHAR(20) NOT NULL UNIQUE
);


/* ================================
   INSERT PARTICIPANTS
   ================================ */

INSERT PARTICIPANT (FirstName, LastName, Email, Phone)
VALUES
('Thabo', 'Mokoena', 'thabo@gmail.com', '+27821234567'),
('Lerato', 'Nkosi', 'lerato@gmail.com', '+27829876543');


/* ================================
   CREATE EVENT TABLE
   ================================ */

CREATE TABLE EVENT (
    EventId INT IDENTITY (1,1) UNIQUE,
    OrganiserId INT NOT NULL,
    EventName VARCHAR(200) NOT NULL,
    EventDate DATE NOT NULL,
    Location VARCHAR(200) NOT NULL,
    EventType VARCHAR(100) NOT NULL,

    FOREIGN KEY (OrganiserId) REFERENCES EVENT_ORGANISER (OrganiserId)
);


/* ================================
   INSERT EVENTS
   ================================ */

INSERT EVENT (OrganiserId, EventName, EventDate, Location, EventType)
VALUES
(1, 'Durban City Marathon', '2026-06-14', 'Durban', 'Running'),
(2, 'Cape Town Cycle Challenge', '2026-03-08', 'Cape Town', 'Cycling'),
(1, 'Soweto Community Walk', '2026-09-21', 'Soweto', 'Walking');


/* ================================
   CREATE CATEGORY TABLE
   ================================ */

CREATE TABLE CATEGORY (
    CategoryId INT IDENTITY (1,1) UNIQUE,
    EventId INT NOT NULL,
    CategoryName VARCHAR(200) NOT NULL,
    Distance DECIMAL(6,2) NOT NULL,

    FOREIGN KEY (EventId) REFERENCES EVENT (EventId)
);


/* ================================
   INSERT CATEGORIES
   ================================ */

INSERT CATEGORY (EventId, CategoryName, Distance)
VALUES
-- Categories for Durban City Marathon
(1, 'Full Marathon', 42.20),
(1, 'Half Marathon', 21.10),
(1, '10km Run', 10.00),

-- Categories for Cape Town Cycle Challenge
(2, 'Long Cycle', 109.00),
(2, 'Medium Cycle', 50.00),
(2, 'Short Cycle', 25.00),

-- Categories for Soweto Community Walk
(3, '10km Walk', 10.00),
(3, '5km Walk', 5.00),
(3, 'Family Walk', 3.00);


/* ================================
   CREATE REGISTRATION TABLE
   ================================ */

CREATE TABLE REGISTRATION (
    RegistrationId INT IDENTITY (1,1) UNIQUE,
    ParticipantId INT NOT NULL,
    EventId INT NOT NULL,
    CategoryId INT NOT NULL,
    RegistrationDate DATE NOT NULL DEFAULT GETDATE(),
    RegistrationStatus VARCHAR(100) NOT NULL DEFAULT 'Registered',

    FOREIGN KEY (ParticipantId) REFERENCES PARTICIPANT (ParticipantId),
    FOREIGN KEY (EventId) REFERENCES EVENT (EventId),
    FOREIGN KEY (CategoryId) REFERENCES CATEGORY (CategoryId)
);


/* ================================
   INSERT REGISTRATIONS
   ================================ */

INSERT REGISTRATION 
(ParticipantId, EventId, CategoryId, RegistrationDate, RegistrationStatus)
VALUES
(1, 1, 1, '2026-01-15', 'Registered'),
(1, 2, 4, '2026-01-20', 'Registered'),
(2, 1, 2, '2026-02-01', 'Registered'),
(2, 3, 8, '2026-02-10', 'Registered');


/* ================================
   CREATE RESULT TABLE
   ================================ */

CREATE TABLE RESULT (
    ResultId INT IDENTITY (1,1) UNIQUE,
    RegistrationId INT NOT NULL UNIQUE,
    FinishTime VARCHAR(20) NOT NULL,
    Position INT NOT NULL,
    Status VARCHAR(100) NOT NULL DEFAULT 'Completed',

    FOREIGN KEY (RegistrationId) REFERENCES REGISTRATION (RegistrationId)
);


/* ================================
   INSERT RESULTS
   ================================ */

INSERT RESULT (RegistrationId, FinishTime, Position, Status)
VALUES
(1, '03:45:22', 15, 'Completed'),
(2, '04:10:35', 30, 'Completed'),
(3, '01:52:10', 8, 'Completed');


/* ================================
   DISPLAY DATA
   ================================ */

SELECT * FROM EVENT_ORGANISER;

SELECT * FROM PARTICIPANT;

SELECT * FROM EVENT;

SELECT * FROM CATEGORY;

SELECT * FROM REGISTRATION;

SELECT * FROM RESULT;