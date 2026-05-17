-- Placeholder. EF Core migrations run on API startup and create all tables,
-- so this file only needs to ensure the database + user exist with the right
-- collation. MySQL's standard init scripts handle the rest.

CREATE DATABASE IF NOT EXISTS cx_platform
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

GRANT ALL PRIVILEGES ON cx_platform.* TO 'cx'@'%';
FLUSH PRIVILEGES;
