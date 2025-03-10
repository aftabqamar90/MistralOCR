# MistralOCR

A .NET Core web application that integrates with Mistral AI's OCR capabilities to process PDF documents.

## Features

- Upload PDF files for OCR processing
- Integration with Mistral AI's OCR API
- Get temporary access URLs for uploaded files (valid for 24 hours)
- Modern Bootstrap UI with responsive design
- Error handling and validation

## Prerequisites

- .NET 9.0 SDK or later
- Mistral AI API key

## Setup

1. Clone the repository
2. Update the `appsettings.json` file with your Mistral AI API key:
   ```json
   "MistralAI": {
     "ApiKey": "YOUR_MISTRAL_API_KEY_HERE"
   }
   ```
3. Alternatively, set the API key as an environment variable:
   - Windows: `setx MISTRAL_API_KEY "your-api-key-here"`
   - Linux/macOS: `export MISTRAL_API_KEY="your-api-key-here"`

## Running the Application

1. Navigate to the project directory
2. Run the application:
   ```
   dotnet run
   ```
3. Open a web browser and navigate to `https://localhost:5001` or `http://localhost:5000`

## Usage

1. On the home page, click the "Choose File" button to select a PDF file
2. Click the "Upload and Process" button to send the file to Mistral AI for OCR processing
3. View the results displayed on the page
4. After successful upload, you'll receive a temporary URL to access the file
   - The URL is valid for 24 hours
   - You can copy the URL to clipboard or open it directly in a new tab

## API Integration

The application integrates with the following Mistral AI APIs:

1. **File Upload API**: `POST https://api.mistral.ai/v1/files`
   - Uploads a PDF file for OCR processing

2. **File URL API**: `GET https://api.mistral.ai/v1/files/{fileId}/url?expiry=24`
   - Gets a temporary URL to access the uploaded file
   - The URL is valid for the specified expiry time (default: 24 hours)

## Environment Variables

The application can use the following environment variables:

- `MISTRAL_API_KEY`: Your Mistral AI API key (alternative to setting it in appsettings.json)

## License

This project is licensed under the MIT License - see the LICENSE file for details. 