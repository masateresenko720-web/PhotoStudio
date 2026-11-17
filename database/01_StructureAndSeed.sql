SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF EXISTS (SELECT 1 FROM sys.tables WHERE is_ms_shipped=0)
    THROW 51000, N'База уже содержит таблицы. Скрипт предназначен для пустой базы; данные не удалены.', 1;
CREATE TABLE dbo.roles (
 role_id int IDENTITY(1,1) CONSTRAINT pk_roles PRIMARY KEY,
 role_name varchar(50) NOT NULL CONSTRAINT uq_role UNIQUE
);
CREATE TABLE dbo.statuses (
 status_id int IDENTITY(1,1) CONSTRAINT pk_statuses PRIMARY KEY,
 status_name varchar(50) NOT NULL CONSTRAINT uq_status UNIQUE
);
CREATE TABLE dbo.users (
 user_id int IDENTITY(1,1) CONSTRAINT pk_users PRIMARY KEY,
 login varchar(50) NOT NULL CONSTRAINT uq_login UNIQUE,
 password_hash varchar(255) NOT NULL,
 last_name varchar(60) NOT NULL,
 first_name varchar(60) NOT NULL,
 middle_name varchar(60) NULL,
 phone varchar(20) NULL,
 role_id int NOT NULL CONSTRAINT fk_users_role REFERENCES dbo.roles(role_id)
);
CREATE TABLE dbo.halls (
 hall_id int IDENTITY(1,1) CONSTRAINT pk_halls PRIMARY KEY,
 hall_name varchar(100) NOT NULL CONSTRAINT uq_hall UNIQUE,
 price_per_hour decimal(10,2) NOT NULL CONSTRAINT ck_hall_price CHECK(price_per_hour>0),
 description text NOT NULL
);
CREATE TABLE dbo.services (
 service_id int IDENTITY(1,1) CONSTRAINT pk_services PRIMARY KEY,
 service_name varchar(100) NOT NULL CONSTRAINT uq_service UNIQUE,
 price decimal(10,2) NOT NULL CONSTRAINT ck_service_price CHECK(price>=0)
);
CREATE TABLE dbo.orders (
 order_id int IDENTITY(1,1) CONSTRAINT pk_orders PRIMARY KEY,
 order_date date NOT NULL CONSTRAINT df_order_date DEFAULT(CONVERT(date,GETDATE())),
 client_id int NOT NULL CONSTRAINT fk_orders_client REFERENCES dbo.users(user_id),
 hall_id int NOT NULL CONSTRAINT fk_orders_hall REFERENCES dbo.halls(hall_id),
 photographer_id int NULL CONSTRAINT fk_orders_photo REFERENCES dbo.users(user_id),
 start_time datetime NOT NULL,
 end_time datetime NOT NULL,
 status_id int NOT NULL CONSTRAINT fk_orders_status REFERENCES dbo.statuses(status_id),
 CONSTRAINT ck_order_interval CHECK(end_time>start_time)
);
CREATE TABLE dbo.order_services (
 order_service_id int IDENTITY(1,1) CONSTRAINT pk_order_services PRIMARY KEY,
 order_id int NOT NULL CONSTRAINT fk_os_order REFERENCES dbo.orders(order_id),
 service_id int NOT NULL CONSTRAINT fk_os_service REFERENCES dbo.services(service_id),
 quantity int NOT NULL CONSTRAINT ck_quantity CHECK(quantity BETWEEN 1 AND 100),
 CONSTRAINT uq_order_service UNIQUE(order_id,service_id)
);
CREATE INDEX ix_orders_hall ON dbo.orders(hall_id,start_time,end_time,status_id);
CREATE INDEX ix_orders_photo ON dbo.orders(photographer_id,start_time,end_time,status_id);
INSERT dbo.roles(role_name) VALUES (N'Администратор'),(N'Фотограф'),(N'Клиент');
INSERT dbo.statuses(status_name) VALUES (N'Новый'),(N'Подтверждён'),(N'Завершён'),(N'Отменён');
INSERT dbo.halls(hall_name,price_per_hour,description) VALUES
(N'Белый',1200,N'Светлый интерьер, большие окна, семейные съёмки.'),
(N'Лофт',1500,N'Кирпичная стена, 60 м², до 6 человек. Импульсный свет.'),
(N'Циклорама',1800,N'Бесшовный фон для каталожной съёмки.'),
(N'Портретный',1000,N'Камерный зал с портретным светом.');
INSERT dbo.services(service_name,price) VALUES
(N'Гримёрное место',500),(N'Реквизит',300),(N'Визаж',2000),(N'Ретушь кадра',250);
INSERT dbo.users(login,password_hash,last_name,first_name,phone,role_id) VALUES ('admin','600000.7vOF457tG3e3vBk1LvSaUw==.z3BD2LCIMQmqeMQY4XVb5Kl46WbaRxdmFbR6xw6xqWE=',N'Администратор',N'Мария','+7 (900) 000-00-00',1);
INSERT dbo.users(login,password_hash,last_name,first_name,phone,role_id) VALUES ('photo1','600000.X1lX12mwtvqelju+QpzzSQ==.Z1D7xN9Km5tX7pV/52DS/BUR/hblrGeLNt2KNGEW6AQ=',N'Иванов',N'Иван','+7 (900) 000-00-00',2);
INSERT dbo.users(login,password_hash,last_name,first_name,phone,role_id) VALUES ('photo2','600000.sVVOEompeO9GawzPGFUofA==.WoFEJ6ncWDQRa38y8siyNbU2L39o7SVli5La6KbJJgY=',N'Смирнова',N'Ольга','+7 (900) 000-00-00',2);
INSERT dbo.users(login,password_hash,last_name,first_name,phone,role_id) VALUES ('client1','600000.UVQBCCpQFjdlUCJ59pRCJA==.zllDbaD7PwSTNpp2E1hX3eQMtxQm1BinUkvHFA0w4E0=',N'Клиент',N'Анна','+7 (900) 000-00-00',3);
INSERT dbo.users(login,password_hash,last_name,first_name,phone,role_id) VALUES ('client2','600000.wQdgvooDTcph+Jgmdz1euw==.m6mUO259HljSZYJj3YkQNUP8to2qpD2EGcqek4kkBpc=',N'Клиент',N'Анна','+7 (900) 000-00-00',3);
INSERT dbo.users(login,password_hash,last_name,first_name,phone,role_id) VALUES ('client3','600000.4MSy/nCd9z6Gks1j7ORvUA==.GZMKWW27Dgs4J0G6Ny0mZhRoAMcn+ygMNpnqrLtKkFg=',N'Клиент',N'Анна','+7 (900) 000-00-00',3);
INSERT dbo.users(login,password_hash,last_name,first_name,phone,role_id) VALUES ('client4','600000.HWKfL8tQY14XNyXY8DTSTQ==.aYEgvZsZHBZ39PH0oCnlXN1apqeiQz/8WGVvggYbR3A=',N'Клиент',N'Анна','+7 (900) 000-00-00',3);
INSERT dbo.users(login,password_hash,last_name,first_name,phone,role_id) VALUES ('client5','600000.r4SrZ1iPbQCBfjquQTda9Q==.QdDEr21qw3i357Yp/b7izchmT9vLjI2cGx2nNIo19io=',N'Клиент',N'Анна','+7 (900) 000-00-00',3);
INSERT dbo.users(login,password_hash,last_name,first_name,phone,role_id) VALUES ('client6','600000.S4in3+cu6PAvNHdao3CbHA==.XU7G9VfuvTK7cp3GFxBCyDVZx8w4+ajfNSBiz4bqZts=',N'Клиент',N'Анна','+7 (900) 000-00-00',3);
INSERT dbo.users(login,password_hash,last_name,first_name,phone,role_id) VALUES ('client7','600000.AqhzjRHwr7u3WnpDz43cIA==./LajHYVNsVRk7QAEYyjhhV6ai9o0O8TqHyrMgUlR8o0=',N'Клиент',N'Анна','+7 (900) 000-00-00',3);
DECLARE @i int=0;
WHILE @i<15
BEGIN
 DECLARE @start datetime=DATEADD(hour,10,CAST(DATEADD(day,-20+@i,CONVERT(date,GETDATE())) AS datetime));
 INSERT dbo.orders(client_id,hall_id,photographer_id,order_date,start_time,end_time,status_id)
 VALUES (4+@i%7,1+@i%4,2+@i%2,CONVERT(date,@start),@start,DATEADD(hour,2,@start),3);
 INSERT dbo.order_services(order_id,service_id,quantity) VALUES(CONVERT(int,SCOPE_IDENTITY()),1,1);
 SET @i=@i+1;
END;
COMMIT;
