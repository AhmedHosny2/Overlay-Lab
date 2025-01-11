# Creating and Configuring Exercises for the Overlay Network

This guide assumes the admin has access to the server VM inside the overlay network. Refer to [section 1.4 of this guide](https://lect.st.hs-ulm.de/dwsys/) for more details.

---

## Step 1: Create and Publish Your Docker Image

### 1.1 Write Your Server Code
- Use your preferred programming language to write a simple server application.
- Choose a port to listen on internally (e.g., `5005`).

### 1.2 Create a `Dockerfile`
- Place the `Dockerfile` in the root directory of your project.
- Add necessary configurations such as the server type, working directory, exposed port, and entry point.

### 1.3 Optimize Your Docker Image
- For better performance:
  1. Split dependencies into a `requirements.txt` file.
  2. Use an `entrypoint.sh` script for handling startup commands.
- This ensures efficient updates when only specific parts of the container change.

### Note on Ports
- The `port` you choose inside the container (e.g., `5005`) is **used internally by Docker**.
- The **server VM** will map this container port (e.g., `5005`) to a dynamically chosen external port that is available on the VM.
- The external port on the server VM is what users will interact with, and this mapping is defined in the exercise configuration.

### Example Resources
- For a complete example, check this [GitHub repository](https://github.com/AhmedHosny2/Http-Get-Server/tree/main).

---

### Step 2: Build and Push Your Docker Image

Run the following commands to build and push your image to Docker Hub:

```bash
docker build -t yourdockerhubusername/imagename:tag .
docker push yourdockerhubusername/imagename:tag
```

Ensure the image is public so it can be accessed by the exercise configuration.

---

## Step 3: Create Exercise Configuration

### 3.1 Navigate to the Project Directory
On the server VM, navigate to the `ExConfiguration` folder:

```bash
cd Portal/ExConfiguration
```

### 3.2 Create a JSON Configuration File
Create a JSON file for your exercise:

```bash
nano Http-Get-Server-Side.json
```

### 3.3 Define Exercise Properties
Write the following configuration:

```json
{
  "ExerciseName": "Http-Get-Server-Side",
  "ExerciseReqConnectionType": "REST",
  "ExerciseTile": "Simple HTTP Get Request simulation", 
  "ExerciseDescription": "This exercise will help you to understand how to send a GET request and receive a response from a server including the current time.",
  "ExerciseDifficulty": "1",
  "DockerImage": "yourdockerhubusername/imagename:tag",
  "port": "5005",
  "DisplayFields": ["Name", "State.Status", "Config.Hostname", "Config.*"],
  "MaxUsers": 5
}
```

- Replace `yourdockerhubusername/imagename:tag` with your Docker Hub image details.
- Replace `"5005"` with the port your container listens to internally.

---

### Note on Server VM Mapping
When running the exercise on the server VM:
1. The VM will dynamically assign an external port for the container.
2. Users will interact with the external port on the VM, not the internal Docker port.

For example:
- Docker container listens on `5005` internally.
- The server VM maps this to an external port like `53217` for users to connect.

---

## Step 4: Simulating Client-Side Exercises

### 4.1 Update Docker Image
For client-side simulations:
1. Modify your server code to:
   - Load the user's IP, port, and UID.
   - Retrieve the container ID.
   - Continuously send requests to the user's IP and port.
   - On success, send an acknowledgment back to the server using the API:
     ```python
     api_url = f"http://localhost:3000/api/ClientEvaluation/{uid}/{container_id}"
     ```

### 4.2 Update JSON Configuration
Add the following entries to your JSON configuration:

```json
"ClientSide": true, 
"ClientPort": "5009"
```

- `ClientSide`: Indicates that the exercise involves client-side interaction.
- `ClientPort`: Specifies the port where users will engage with the container.

---

## Example Client Code

For a simple client implementation, refer to this [GitHub repository](https://github.com/AhmedHosny2/Http-Get-Client). It provides a complete example of how a client application interacts with the server.

---

## Your Exercise is Ready!

Follow these steps to set up and configure exercises for your overlay network. You can now deploy and use the exercise in your environment. 🎉