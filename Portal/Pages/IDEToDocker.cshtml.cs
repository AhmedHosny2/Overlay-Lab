using System.Text;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Antiforgery;
namespace Portal.Pages;

public class IDEToDockerModel : PageModel
{
    [BindProperty]
    public string CommandInput { get; set; }
    [BindProperty]
    public string CommandOutput { get; set; }
    //TODO make this function once and use it in all pages not twice  
    public DockerClient ConnectToDocker()
    {
        // create a new DockerClient object 
        DockerClient client = new DockerClientConfiguration(
      new Uri("unix:///var/run/docker.sock")) // todo what is this ? 
       .CreateClient();
        return client;
    }
    public async Task<StringBuilder> ExecuteCommand(DockerClient client, List<string> Command)
    {


        var myCreatedContainerId = HttpContext.Session.GetString("CreatedContainerId");

        Console.WriteLine($"Client: {client}, CreatedContainer: {myCreatedContainerId}, Command: {string.Join(" ", Command)}");
        try
        {
            var execCreateResponse = await client.Exec.ExecCreateContainerAsync(myCreatedContainerId, new ContainerExecCreateParameters
            {
                AttachStdin = true,
                AttachStdout = true,
                AttachStderr = true,
                // Tty = true,        
                // command create directory in the root add file to it and list the files in it

                Cmd = Command ?? new List<string> { "sh", "-c", "mkdir /root/test && echo 'Hello, World!' > /root/test/hello.txt && ls /root/test" }
            });

            // Start the exec instance and attach to the output
            using (var stream = await client.Exec.StartAndAttachContainerExecAsync(execCreateResponse.ID, false))
            {
                var outputBuilder = new StringBuilder();
                var buffer = new byte[4096];

                // Read the output synchronously
                while (true)
                {
                    var count = await stream.ReadOutputAsync(buffer, 0, buffer.Length, CancellationToken.None);
                    if (count.EOF)
                    {
                        break;
                    }

                    outputBuilder.Append(Encoding.UTF8.GetString(buffer, 0, count.Count));
                }

                // Print the output to the console
                Console.WriteLine("Command output:");
                Console.WriteLine(outputBuilder);
                // convert ansi to html
                // var ansi = new AnsiToHtml();
                // HtmlOutput = ansi.Convertrgb(146, 34, 34)(outputBuilder.ToString());

                return outputBuilder;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: " + e.Message);
            return new StringBuilder(e.Message);
        }
    }




    public void OnGet()
    {
    }
    public async Task<IActionResult> OnPostExecuteCommand()
    {
        // get input from the form
        List<string> command = Request.Form["CommandInput"].ToString().Split(' ').ToList();
        // Execute command and get the output
        StringBuilder execOutput = await ExecuteCommand(ConnectToDocker(), command);

        // Store the output in CommandOutput
        CommandOutput = execOutput.ToString();

        // Return the page with updated model
        return Page();
    }


}
