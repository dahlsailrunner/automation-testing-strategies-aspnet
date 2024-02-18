# Automated Testing Strategies for ASP.NET Core

## Getting Started

You should set both the `Api` and the `WebApp` projects as startup projects.

The solution uses two services that it expects to be running locally. The simplest way to
run these services is to use Docker.  The following commands will start the services.

```bash
docker pull rnwood/smpt4dev
docker run --rm -d --name fakemail -p 3000:80 -p 2525:25 rnwood/smtp4dev

docker pull datalust/seq
docker run --name seq -d --restart unless-stopped -e ACCEPT_EULA=Y -p 5341:80 datalust/seq
```

But then you should be able to simply run the solution.

To see logs, you can navigate to [http://localhost:5341](http://localhost:5341).

To see emails, you can navigate to [http://localhost:3000](http://localhost:3000).

## Features

This is a simple e-commerce application that has a few features
that we want to explore automated testing strategies for.

Here are the features:

- **API**
  - `GET` based on category (or "all") and by id allow anonymous requests
  - `POST` requires authentication and an `admin` role
  - Validation will be done and errors returned as a `400 Bad Request` with `ProblemDetails`
  - A `GET` with a category of something other than "all", "boots", "equip", or "kayak" will return a `500 internal server error` with `ProblemDetails`
  - Data is refreshed to a known state as the app starts
- Authentication provided by OIDC via the [demo Duende Identity Server](https://demo.duendesoftware.com)
- A custom claims transformer will add the `admin` role to "Bob Smith" and any authentication via Google
- **WebApp**
  - The home page and listing pages will show a subset of products
  - There is a page at `/Admin` that will show a list of products that can be edited or added to
  - If you navigate to `/Admin` without the admin role, you should see an `AccessDenied` page
  - Any validation errors from the API should be displayed on the admin section edit pages
  - Can add items to cart and see a summary of the cart (shows when empty too)
  - Can submit an order or cancel the order and clear the cart
  - A submitted order will send a fake email

"Catch" with a refactoring change:

- change the "admin" role to be "administrator" and see what breaks
(maybe make it a different claim type than role and change to a "policy"??)

For the learner:

- Add edit and (soft) delete to the API and WebApp, then write tests
- More complex "cart edit" functionality
- Be able to apply a "promotion" on the Cart page

## VS Code Setup

RUnning in VS Code is a totally legitimate use-case for this solution and
repo.

The same instructions above (Getting Started) apply here, but the following
extensions should probably be installed:

- [C# (the language extension)](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp)
- [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit)

Then run the API project and the UI project.

## EF Core Migrations

The `dotnet ef` tool is used to manage EF Core migrations.  The following command is used to create migrations (from the `CarvedRock.Data` folder).

```bash
dotnet ef migrations add Initial -s ../CarvedRock.Api
```

## Verifiying Emails

The very simple email functionality is done using a template
from [this GitHub repo](https://github.com/leemunroe/responsive-html-email-template)
and the [smtp4dev](https://github.com/rnwood/smtp4dev)
service that can easily run in a Docker container.

There is a UI that you can naviagte to in your browser for
seeing the emails that works great.  If you use the `docker run` command
that I have listed above, the UI is at
[http://localhost:3000](http://localhost:3000).

There is also an API that is part of that service and a couple of quick
API calls call give you the content of the email body that you
want to verify:

```bash
GET http://localhost://3000/api/messages

### find the ID of the message you care about

GET http://localhost://3000/api/messages/<message-guid>/html
```
