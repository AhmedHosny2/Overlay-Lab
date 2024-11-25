// ContainerIDEScript.js

// Default content for each language
const defaultContent = {
  javascript: `// JavaScript Example
function greet(name) {
  console.log('Hello, ' + name + '!');
}

greet('World');`,
  python: `# Python Example
def greet(name):
  print(f"Hello, {name}!")

greet("World")`,
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
});

// Handle Theme Change
const themeSelect = document.getElementById("theme-select");
themeSelect.addEventListener("change", function () {
  const theme = this.value;
  editor.setOption("theme", theme);
});

// Initialize with default content
editor.setValue(defaultContent[languageSelect.value] || "// Start coding...");

// Copy to Clipboard Function
function copyToClipboard(elementId) {
  const text = document.getElementById(elementId).innerText;
  navigator.clipboard.writeText(text).then(
    function () {
      alert("Copied to clipboard: " + text);
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

// Initialize Pyodide
let pyodide = null;
const loadingOverlay = document.getElementById("loading-overlay");

async function initializePyodide() {
  try {
    loadingOverlay.style.display = "flex";
    pyodide = await loadPyodide();
    loadingOverlay.style.display = "none";
    console.log("Pyodide loaded successfully.");
  } catch (error) {
    loadingOverlay.textContent = "Failed to load Pyodide.";
    console.error("Error loading Pyodide:", error);
  }
}

initializePyodide();

// Run Button Functionality
document.getElementById("run-button").addEventListener("click", runCode);

async function runCode() {
  const code = editor.getValue();
  const language = languageSelect.value;
  const outputElement = document.getElementById("output");
  outputElement.textContent = ""; // Clear previous output

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
  }
}

// Function to execute JavaScript code
function executeJavaScript(code, outputElement) {
  const logs = [];
  const originalConsoleLog = console.log;
  const originalConsoleError = console.error;

  // Override console.log and console.error
  console.log = function (...args) {
    logs.push(args.map((arg) => formatOutput(arg)).join(" "));
  };
  console.error = function (...args) {
    logs.push("Error: " + args.map((arg) => formatOutput(arg)).join(" "));
  };

  // Async function to allow use of await
  const AsyncFunction = Object.getPrototypeOf(async function () {}).constructor;

  try {
    const userFunction = new AsyncFunction(code);
    userFunction()
      .then(() => {
        outputElement.textContent = logs.join("\n");
        // Restore original console functions
        console.log = originalConsoleLog;
        console.error = originalConsoleError;
      })
      .catch((error) => {
        outputElement.textContent = "Error: " + error;
        console.log = originalConsoleLog;
        console.error = originalConsoleError;
      });
  } catch (error) {
    outputElement.textContent = "Error: " + error;
    console.log = originalConsoleLog;
    console.error = originalConsoleError;
  }
}

// Function to execute Python code using Pyodide
async function executePython(code, outputElement) {
  if (!pyodide) {
    outputElement.textContent = "Error: Pyodide is not loaded yet.";
    return;
  }

  // Redirect stdout and stderr
  let output = "";
  pyodide.setStdout({
    batched: (text) => {
      output += text;
    },
  });
  pyodide.setStderr({
    batched: (text) => {
      output += text;
    },
  });

  try {
    // Allow access to js module for HTTP requests
    pyodide.globals.set("js", pyodide.pyimport("js"));
    await pyodide.runPythonAsync(code);
    outputElement.textContent = output;
  } catch (error) {
    outputElement.textContent = "Error: " + error;
  }
}

// Function to format output (handles objects)
function formatOutput(output) {
  if (typeof output === "object") {
    try {
      return JSON.stringify(output, null, 2);
    } catch (e) {
      return String(output);
    }
  } else {
    return String(output);
  }
}

// Initialize the editor with sample code
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
