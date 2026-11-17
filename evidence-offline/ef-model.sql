CREATE TABLE [halls] (
    [hall_id] int NOT NULL IDENTITY,
    [hall_name] varchar(100) NOT NULL,
    [price_per_hour] decimal(10,2) NOT NULL,
    [description] text NOT NULL,
    CONSTRAINT [PK_halls] PRIMARY KEY ([hall_id]),
    CONSTRAINT [CK_HallPrice] CHECK (price_per_hour > 0)
);
GO


CREATE TABLE [roles] (
    [role_id] int NOT NULL IDENTITY,
    [role_name] varchar(50) NOT NULL,
    CONSTRAINT [PK_roles] PRIMARY KEY ([role_id])
);
GO


CREATE TABLE [services] (
    [service_id] int NOT NULL IDENTITY,
    [service_name] varchar(100) NOT NULL,
    [price] decimal(10,2) NOT NULL,
    CONSTRAINT [PK_services] PRIMARY KEY ([service_id]),
    CONSTRAINT [CK_ServicePrice] CHECK (price >= 0)
);
GO


CREATE TABLE [statuses] (
    [status_id] int NOT NULL IDENTITY,
    [status_name] varchar(50) NOT NULL,
    CONSTRAINT [PK_statuses] PRIMARY KEY ([status_id])
);
GO


CREATE TABLE [users] (
    [user_id] int NOT NULL IDENTITY,
    [login] varchar(50) NOT NULL,
    [password_hash] varchar(255) NOT NULL,
    [last_name] varchar(60) NOT NULL,
    [first_name] varchar(60) NOT NULL,
    [middle_name] varchar(60) NULL,
    [phone] varchar(20) NULL,
    [role_id] int NOT NULL,
    CONSTRAINT [PK_users] PRIMARY KEY ([user_id]),
    CONSTRAINT [FK_users_roles_role_id] FOREIGN KEY ([role_id]) REFERENCES [roles] ([role_id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [orders] (
    [order_id] int NOT NULL IDENTITY,
    [order_date] date NOT NULL DEFAULT (CONVERT(date,GETDATE())),
    [client_id] int NOT NULL,
    [hall_id] int NOT NULL,
    [photographer_id] int NULL,
    [start_time] datetime NOT NULL,
    [end_time] datetime NOT NULL,
    [status_id] int NOT NULL,
    CONSTRAINT [PK_orders] PRIMARY KEY ([order_id]),
    CONSTRAINT [CK_OrderInterval] CHECK (end_time > start_time),
    CONSTRAINT [FK_orders_halls_hall_id] FOREIGN KEY ([hall_id]) REFERENCES [halls] ([hall_id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_orders_statuses_status_id] FOREIGN KEY ([status_id]) REFERENCES [statuses] ([status_id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_orders_users_client_id] FOREIGN KEY ([client_id]) REFERENCES [users] ([user_id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_orders_users_photographer_id] FOREIGN KEY ([photographer_id]) REFERENCES [users] ([user_id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [order_services] (
    [order_service_id] int NOT NULL IDENTITY,
    [order_id] int NOT NULL,
    [service_id] int NOT NULL,
    [quantity] int NOT NULL,
    CONSTRAINT [PK_order_services] PRIMARY KEY ([order_service_id]),
    CONSTRAINT [CK_ExtraQuantity] CHECK (quantity BETWEEN 1 AND 100),
    CONSTRAINT [FK_order_services_orders_order_id] FOREIGN KEY ([order_id]) REFERENCES [orders] ([order_id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_order_services_services_service_id] FOREIGN KEY ([service_id]) REFERENCES [services] ([service_id]) ON DELETE NO ACTION
);
GO


CREATE UNIQUE INDEX [IX_halls_hall_name] ON [halls] ([hall_name]);
GO


CREATE UNIQUE INDEX [IX_order_services_order_id_service_id] ON [order_services] ([order_id], [service_id]);
GO


CREATE INDEX [IX_order_services_service_id] ON [order_services] ([service_id]);
GO


CREATE INDEX [IX_orders_client_id] ON [orders] ([client_id]);
GO


CREATE INDEX [IX_orders_hall_id_start_time_end_time_status_id] ON [orders] ([hall_id], [start_time], [end_time], [status_id]);
GO


CREATE INDEX [IX_orders_photographer_id_start_time_end_time_status_id] ON [orders] ([photographer_id], [start_time], [end_time], [status_id]);
GO


CREATE INDEX [IX_orders_status_id] ON [orders] ([status_id]);
GO


CREATE UNIQUE INDEX [IX_roles_role_name] ON [roles] ([role_name]);
GO


CREATE UNIQUE INDEX [IX_services_service_name] ON [services] ([service_name]);
GO


CREATE UNIQUE INDEX [IX_statuses_status_name] ON [statuses] ([status_name]);
GO


CREATE UNIQUE INDEX [IX_users_login] ON [users] ([login]);
GO


CREATE INDEX [IX_users_role_id] ON [users] ([role_id]);
GO


