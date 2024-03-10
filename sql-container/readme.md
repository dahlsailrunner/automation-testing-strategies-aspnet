# Custom SQL Server Container

This folder contains a Dockerfile that can be used
to build a custom SQL Server container image.

The image that gets created is based on the official SQL Server image.

It runs some custom SQL scripts to create a
database and add some data.  It can be useful for
testing and development purposes to create a pullable
image that has a database and data already set up.

**WARNING:** Don't use this kind of logic for production
databases or databases you want backed up and recoverable.  This
is great for known-state startup logic.

## How It Works (In Brief)

At container startup time, it will run the following SQL scripts based on
the logic in `setup.sh` (add more if you want):

* `CreateDatabase.sql` - Create a database called `CarvedRock`
* `InitializeDatabase.sql` - Apply the schema (tables) to the CarvedRock database
* `InsertSomeData.sql` - Insert some rows for initial testing

If you have some automated logic that could generate the SQL files
or somehow push new SQL files into a repo, that could trigger a
pipline that would push a new Docker image based on the logic
here into a container registry that would be available for
anyone to pull.

The only `.sh` script you should need to modify (possibly) is
the `setup.sh` one.  Maybe you want to use a different database
or invoke different scripts.  The other scripts just wire things
up and wait for SQL to be "ready" before applying the SQL files.

## Building the Image

Use the following command (don't forget the `.` on the end which sets directory context!):

```bash
docker build -t carvedrock/sqlserver .
```

Then you should be able to run the container with the following command:

```bash
docker run -d -p 1434:1433 -e "SA_PASSWORD=1nnerLoop-FTW!" localhost/carvedrock/sqlserver
```

I'm using a port mapping of `1434:1433` to avoid a conflict of using
a host port of `1433` which may already be running if
you have another instance of SQL Server running locally.

## Creating the T-SQL

Ultimately the SQL in the SQL files can be whatever you want or
need it to be.

The `CreateDatabase.sql` script was created manually.

I created the `InitializeDatabase.sql` with the EF Core CLI:

```bash
dotnet ef migrations script -s ../CarvedRock.Api -o ../sql-container/InitializeDatabase.sql
```

The `InsertSomeData.sql` script was created manually.

## Editing the Scripts

If you edit any of the `.sh` scripts in Windows / Visual Studio,
you may need to change the line endings to LF (Unix)
style (you can use [Notepad++](https://notepad-plus-plus.org/) to do
this easily -- `Edit->EOL Conversion->Unix`).

## Pushing to a Container Registry

To push this container image into a registry, you would use the
`docker push` command after doing `docker build` with the tag
you want to use for the pushed image.

The tag I'm using above is `carvedrock/sqlserver` as indicated
by the `-t` option.  Your tag would almost certainly be prefixed
by the registry you want to use.
