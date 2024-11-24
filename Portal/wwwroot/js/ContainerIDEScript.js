// Default content for each language
const defaultContent = {
    'javascript': `// JavaScript Example
  function greet(name) {
    console.log('Hello, ' + name + '!');
  }
  
  greet('World');`,
    'python': `# Python Example
  def greet(name):
      print(f"Hello, {name}!")
  
  greet("World")`,
    'go': `// Go Example
  package main
  
  import "fmt"
  
  func main() {
      fmt.Println("Hello, World!")
  }`,
    'text/x-java': `// Java Example
  public class Main {
      public static void main(String[] args) {
          System.out.println("Hello, World!");
      }
  }`,
    'text/x-c++src': `// C++ Example
  #include <iostream>
  
  int main() {
      std::cout << "Hello, World!" << std::endl;
      return 0;
  }`,
    'ruby': `# Ruby Example
  def greet(name)
    puts "Hello, #{name}!"
  end
  
  greet("World")`,
    'application/x-httpd-php': `<?php
  // PHP Example
  function greet($name) {
      echo "Hello, " . $name . "!";
  }
  
  greet("World");
  ?>`,
    'text/x-csharp': `// C# Example
  using System;
  
  class Program {
      static void Main() {
          Console.WriteLine("Hello, World!");
      }
  }`,
    'htmlmixed': `<!-- HTML Example -->
  <!DOCTYPE html>
  <html lang="en">
  <head>
      <meta charset="UTF-8">
      <title>Hello World</title>
  </head>
  <body>
      <h1>Hello, World!</h1>
  </body>
  </html>`,
    'css': `/* CSS Example */
  body {
      background-color: #282a36;
      color: #f8f8f2;
      font-family: Arial, sans-serif;
  }`
  };
  
  // Initialize CodeMirror
  const editor = CodeMirror.fromTextArea(document.getElementById('code-editor'), {
    lineNumbers: true,
    mode: 'javascript',
    theme: 'dracula',
    lineWrapping: true,
    styleActiveLine: true,
    matchBrackets: true,
    autoCloseBrackets: true,
    tabSize: 2,
    indentWithTabs: false,
    extraKeys: {
      "Ctrl-Space": "autocomplete"
    }
  });
  
  // Handle Language Change
  const languageSelect = document.getElementById('language-select');
  languageSelect.addEventListener('change', function() {
    const mode = this.value;
    editor.setOption('mode', mode);
    if (defaultContent[mode]) {
      editor.setValue(defaultContent[mode]);
    } else {
      editor.setValue('// Start coding...');
    }
  });
  
  // Handle Theme Change
  const themeSelect = document.getElementById('theme-select');
  themeSelect.addEventListener('change', function() {
    const theme = this.value;
    editor.setOption('theme', theme);
  });
  
  // Initialize with default content
  editor.setValue(defaultContent[languageSelect.value] || '// Start coding...');
  
  // Run Button Functionality
  const runButton = document.getElementById('run-button');
  runButton.addEventListener('click', function() {
    alert('Your code will be executed. (Placeholder function)');
  });