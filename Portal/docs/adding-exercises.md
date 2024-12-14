# How to Add an Exercise to the Website

Follow these simple steps to add a new exercise to the website. This guide assumes you have a programming background and Docker installed.

---

## Docker Image

### Step 1: Set Up Docker
Make sure Docker is installed on your machine. You can download it from the [official Docker website](https://www.docker.com/products/docker-desktop) or use your system's package manager.

### Step 2: Write the Server Code
Create your server code that the exercise will use. It can be in any language, as long as it's compatible with Docker.

### Step 3: Create a `Dockerfile`
Write a `Dockerfile` to containerize your server. Here's an example for a Python-based app:

```dockerfile
FROM python:3.8-slim
WORKDIR /app
COPY requirements.txt .
RUN pip install -r requirements.txt
COPY . .
CMD ["python", "app.py"]```

Step 4: Build the Docker Image

Build the Docker image using the docker build command:

docker build -t your-dockerhub-username/image-name .

Step 5: Push the Image to a Public Repository

Push the image to Docker Hub (or any public container registry):

docker push your-dockerhub-username/image-name

Create a Configuration File

Step 1: Navigate to the ExConfiguration Directory

Change into the ExConfiguration directory where exercise configuration files are stored:

cd ExConfiguration

Step 2: Create a New Configuration File

Create a new JSON file for your exercise. You can use your preferred method:
	•	CLI Option: Use a command like nano:

nano ExerciseName.json


	•	GUI Option: Use your file manager or editor to create a new JSON file.

Step 3: Add Metadata and Container Data

Fill out the metadata for your exercise in the JSON file. Here’s an example:

{
  "ExerciseName": "Get-Time",
  "ExerciseReqConnectionType": "REST",
  "ExerciseTile": "Simple HTTP Get Request Simulation",
  "ExerciseDescription": "This exercise will help you understand how to send a GET request and receive a response from a server, including the current time.",
  "ExerciseDifficulty": "1",
  "DockerImage": "your-dockerhub-username/time_app",
  "port": "5005",
  "DisplayFields": [
    "Name",
    "State.Status",
    "Config.Hostname"
  ]
}

	•	ExerciseName: The unique name of the exercise.
	•	ExerciseReqConnectionType: Type of connection (e.g., REST, gRPC).
	•	ExerciseTile: Short title describing the exercise.
	•	ExerciseDescription: Detailed description of the exercise’s purpose.
	•	ExerciseDifficulty: Difficulty level (e.g., 1 for easy, 5 for hard).
	•	DockerImage: The image name you created earlier.
	•	port: The port your container will expose.
	•	DisplayFields: Data to display about the container, such as Name, State.Status, or Config.Hostname. Use docker inspect to identify the fields you need.

Get Container Data for Display

To retrieve container details, inspect the container:
	•	Using Docker Desktop: View the container details directly in the UI.
	•	Using CLI: Run the following command:

docker inspect IMAGE_NAME



You can specify a field directly in the DisplayFields. For example:
	•	"Config" will return a JSON object.
	•	"Config.Hostname" will return a specific string.

Final Steps
	1.	Push the JSON File: Push the configuration file to your codebase repository.
	2.	Replace Real Data with Dummy Values: For sensitive fields (e.g., port, hostname), replace real values with dummy ones before committing the file.

That’s it! You’ve successfully added a new exercise to the website.
