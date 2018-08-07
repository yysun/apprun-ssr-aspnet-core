# apprun-ssr-aspnet-core

Use [AppRun](https://github.com/yysun/apprun) to make ASP.NET MVC Core applications into single-page applications (SPA).

Accessing the page directly, the app returns HTML (server-side rendering).
![html](html.png)

Then AppRun switches it into single-page application mode. The app returns the virtual DOM as JSON for AppRun to render the real DOM.
![vdom](vdom.png)

### TDDO: Create a Nuget package
