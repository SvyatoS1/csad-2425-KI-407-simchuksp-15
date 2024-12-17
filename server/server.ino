void setup() {
  Serial.begin(9600);  // Initialize serial communication at 9600 baud rate
}

void loop() {
  // Check if data is available to read
  if (Serial.available() > 0) {
    String receivedMessage = Serial.readStringUntil('\n');  // Read the message from client
    String responseMessage = modifyMessage(receivedMessage);  // Modify the message
    Serial.println(responseMessage);  // Send the modified message back to client
  }
}

// This function modifies the received message (for demonstration purposes)
String modifyMessage(String message) {
  // Example modification: Append a fixed string
  return message + " - Processed by Arduino";
}
