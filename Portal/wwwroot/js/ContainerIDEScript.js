// ContainerIDEScript.js

// Default content for each language
const defaultContent = {
  javascript: `// JavaScript Example
function greet(name) {
  console.log('Hello, ' + name + '!');
}

greet('World');

// Example of an HTTP request
fetch('https://jsonplaceholder.typicode.com/posts/1')
  .then(response => response.json())
  .then(data => {
    console.log('Fetched Data:', data);
  })
  .catch(error => {
    console.error('Fetch Error:', error);
  });`,
  python: `# Python Example
def greet(name):
    print(f"Hello, {name}!")

greet("World")

# Example of an HTTP request using requests
import requests

try:
    response = requests.get("https://jsonplaceholder.typicode.com/posts/1")
    response.raise_for_status()
    data = response.json()
    print("Fetched Data:", data)
except requests.exceptions.RequestException as e:
    print("Fetch Error:", e)`,
};

// Initialize CodeMirror
const editor = CodeMirror.fromTextArea(document.getElementById("code-editor"), {
  lineNumbers: true,
  mode: "javascript",
  theme: "dracula",
  lineWrapping: true,
  styleActiveLine: true,
  matchBrackets: true,
  autoCloseBrackets: true,
  tabSize: 2,
  indentWithTabs: false,
  extraKeys: {
    "Ctrl-Space": "autocomplete",
  },
});

// Handle Language Change
const languageSelect = document.getElementById("language-select");
languageSelect.addEventListener("change", function () {
  const mode = this.value;
  editor.setOption("mode", mode);
  if (defaultContent[mode]) {
    editor.setValue(defaultContent[mode]);
  } else {
    editor.setValue("// Start coding...");
  }
  // Reset theme if necessary
  // editor.setOption("theme", themeSelect.value);
});

// Handle Theme Change
// const themeSelect = document.getElementById("theme-select");
// themeSelect.addEventListener("change", function () {
//   const theme = this.value;
//   editor.setOption("theme", theme);
// });

// Initialize with default content
editor.setValue(defaultContent[languageSelect.value] || "// Start coding...");

// Copy to Clipboard Function
function copyToClipboard(elementId) {
  const text = document.getElementById(elementId).innerText;
  navigator.clipboard.writeText(text).then(
    function () {
      alert("Output copied to clipboard!");
    },
    function (err) {
      console.error("Could not copy text: ", err);
    }
  );
}

// Handle Copy Buttons
const copyButtons = document.querySelectorAll(".copy-button");
copyButtons.forEach((button) => {
  button.addEventListener("click", function () {
    const targetId = this.getAttribute("data-copy-target");
    copyToClipboard(targetId);
  });
});

// Initialize Pyodide to run Python code
let pyodide = null;
const loadingOverlay = document.getElementById("loading-overlay");

async function initializePyodide() {
  try {
    loadingOverlay.style.display = "flex";
    pyodide = await loadPyodide({
      indexURL: "https://cdn.jsdelivr.net/pyodide/v0.23.4/full/",
    });
    loadingOverlay.style.display = "none";
    console.log("Pyodide loaded successfully.");
  } catch (error) {
    loadingOverlay.textContent = "Failed to load Pyodide.";
    console.error("Error loading Pyodide:", error);
  }
}

initializePyodide();

// Append message to output area
function appendOutput(message) {
  const outputElement = document.getElementById("output");
  // Escape HTML to prevent injection
  const escapedMessage = message
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;");
  outputElement.textContent += escapedMessage + "\n";
  // Auto-scroll to the bottom
  outputElement.scrollTop = outputElement.scrollHeight;
}

// Function to format output (handles objects and other data types)
function formatOutput(output) {
  if (typeof output === "object" && output !== null) {
    try {
      return JSON.stringify(output, null, 2);
    } catch (e) {
      return String(output);
    }
  } else {
    return String(output);
  }
}

// Function to override console methods
function overrideConsole() {
  if (!console.originalLog) {
    console.originalLog = console.log;
    console.originalError = console.error;

    console.log = function (...args) {
      const message = args.map((arg) => formatOutput(arg)).join(" ");
      appendOutput(message);
      console.originalLog.apply(console, args);
    };

    console.error = function (...args) {
      const message =
        "Error: " + args.map((arg) => formatOutput(arg)).join(" ");
      appendOutput(message);
      console.originalError.apply(console, args);
    };
  }
}

// Function to restore console methods
function restoreConsole() {
  if (console.originalLog) {
    console.log = console.originalLog;
    console.error = console.originalError;
    delete console.originalLog;
    delete console.originalError;
  }
}

// Run Button Functionality
document.getElementById("run-button").addEventListener("click", runCode);

async function runCode() {
  const code = editor.getValue();
  const language = languageSelect.value;
  const outputElement = document.getElementById("output");

  // Clear previous output
  outputElement.textContent = "";

  // Restore console if previously overridden
  restoreConsole();

  // Override console to capture logs
  overrideConsole();

  // Show loading overlay
  loadingOverlay.style.display = "flex";

  // Supported client-side languages
  const clientSideLanguages = ["javascript", "python"];

  if (clientSideLanguages.includes(language)) {
    if (language === "javascript") {
      executeJavaScript(code, outputElement);
    } else if (language === "python") {
      await executePython(code, outputElement);
    }
  } else {
    // Alert the user if an unsupported language is selected
    alert("This language is not supported yet.");
    // Restore console and hide loading overlay
    restoreConsole();
    loadingOverlay.style.display = "none";
  }
}

// Function to execute JavaScript code
function executeJavaScript(code, outputElement) {
  // Async function to allow use of await within user code
  const AsyncFunction = Object.getPrototypeOf(async function () {}).constructor;

  try {
    const userFunction = new AsyncFunction(code);
    userFunction()
      .then(() => {
        // Hide loading overlay after execution completes
        loadingOverlay.style.display = "none";
        // Restore console to prevent capturing logs from future operations
        // Commented out to keep console overridden for async logs
        // restoreConsole();
      })
      .catch((error) => {
        appendOutput("Error: " + formatOutput(error));
        // Hide loading overlay
        loadingOverlay.style.display = "none";
        // Restore console
        restoreConsole();
      });
  } catch (error) {
    appendOutput("Error: " + formatOutput(error));
    // Hide loading overlay
    loadingOverlay.style.display = "none";
    // Restore console
    restoreConsole();
  }
}

// Function to execute Python code using Pyodide
async function executePython(code, outputElement) {
  if (!pyodide) {
    appendOutput("Error: Pyodide is not loaded yet.");
    loadingOverlay.style.display = "none";
    // Restore console
    restoreConsole();
    return;
  }

  // Redirect stdout and stderr
  let output = "";
  const stdout = (text) => {
    output += text;
  };
  const stderr = (text) => {
    output += "Error: " + text;
  };

  pyodide.setStdout({
    batched: (text) => {
      appendOutput(formatOutput(text));
    },
  });
  pyodide.setStderr({
    batched: (text) => {
      appendOutput("Error: " + formatOutput(text));
    },
  });

  try {
    // Allow access to js module for HTTP requests if needed
    pyodide.globals.set("js", pyodide.pyimport("js"));
    await pyodide.runPythonAsync(code);
    // Hide loading overlay after execution
    loadingOverlay.style.display = "none";
    // Restore console
    restoreConsole();
  } catch (error) {
    appendOutput("Error: " + formatOutput(error));
    // Hide loading overlay
    loadingOverlay.style.display = "none";
    // Restore console
    restoreConsole();
  }
}

// Initialize the editor with sample code on window load
window.addEventListener("load", () => {
  const language = languageSelect.value;
  if (defaultContent[language]) {
    editor.setValue(defaultContent[language]);
  }
});

// Update the editor content when the language changes
languageSelect.addEventListener("change", () => {
  const language = languageSelect.value;
  if (defaultContent[language]) {
    editor.setValue(defaultContent[language]);
  } else {
    editor.setValue("// Start coding...");
  }
});
