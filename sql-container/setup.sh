# Wait for SQL Server to be started and then run the sql script
./wait-for-it.sh localhost:1433 --timeout=0 --strict -- sleep 5s && \

/opt/mssql-tools/bin/sqlcmd -S localhost -i CreateDatabase.sql -U sa -P "$SA_PASSWORD"
/opt/mssql-tools/bin/sqlcmd -S localhost -i InitializeDatabase.sql -d CarvedRock -U sa -P "$SA_PASSWORD"
/opt/mssql-tools/bin/sqlcmd -S localhost -i InsertSomeData.sql -d CarvedRock -U sa -P "$SA_PASSWORD"