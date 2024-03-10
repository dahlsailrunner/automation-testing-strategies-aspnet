FROM mcr.microsoft.com/mssql/server 
USER root

ENV ACCEPT_EULA=Y
ENV MSSQL_PID Express

ARG PROJECT_DIR=/tmp/carvedrock
RUN mkdir -p $PROJECT_DIR
WORKDIR $PROJECT_DIR
COPY ./*.sql ./
COPY ./*.sh ./
RUN chmod +x *.sh

CMD ["/bin/bash", "entrypoint.sh"]