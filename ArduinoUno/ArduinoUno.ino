#include <WiFiS3.h>
#include "secrets.h"
#include "DHT.h"


#define DHTPIN A4       // DHT11 data pin
#define DHTTYPE DHT11   // DHT 11
const int senLED = 7;
const int dustSen = A1;
const int UUID = 2831172;

uint16_t calcCRC(const uint8_t* data, size_t length, uint16_t startCrc = 0xFFFF) {
  uint16_t crc = startCrc;
for (int i = length - 1; i >= 0; --i) {
  crc ^= (uint16_t)(data[i] << 8);
  for (int j = 0; j < 8; ++j) {
    if (crc & 0x8000)
      crc = (crc << 1) ^ 0x1021;
    else
      crc <<= 1;
  }
}
  
  return crc;
}



float voMeasured = 0;
float calcVoltage = 0;
float readT[5];
float readH[5];
float readD[5];
short measurementCount = 0;
short currentCount = 0;
String message;

DHT dht(DHTPIN, DHTTYPE);


const char* ssid = SSID;
const char* password = PASS;


const char* serverIP = "10.199.168.120";  
const uint16_t serverPort = 12345;

WiFiClient client;

void setup() {
  Serial.begin(9600);
  
  dht.begin();
  pinMode(senLED, OUTPUT);
  pinMode(dustSen, INPUT);
  
  while (!Serial);

  // Connect to WiFi
  Serial.print("Connecting to WiFi...");
  while (WiFi.begin(ssid, password) != WL_CONNECTED) {
    delay(1000);
    Serial.print(".");
  }
  Serial.println(" connected!");

  Serial.print("Connecting to server...");
  if (client.connect(serverIP, serverPort)) {
    Serial.println(" connected!");
  }

  


}

 

void loop() {

  if (!client.connected()){
    Serial.print("Connecting to server...");
    if (client.connect(serverIP, serverPort)) {
      Serial.println(" connected!");
    }
  }

  float h = dht.readHumidity();
  float t = dht.readTemperature();
  digitalWrite(senLED, HIGH);
  delayMicroseconds(280);
  voMeasured = analogRead(dustSen);
  delayMicroseconds(40);
  digitalWrite(senLED, LOW);
  delayMicroseconds(9680);

  calcVoltage = voMeasured * (5.0 / 1024.0);
  float d = 170 * calcVoltage - 0.1;

  readT[currentCount] = t;
  readH[currentCount] = h;
  readD[currentCount] = d;
  if (measurementCount < 5){
    measurementCount++;
    }
  else{
      measurementCount = 5;
    }

  if (currentCount < 5){
    currentCount++;
    }
    else{
      currentCount = 0;
    }

  message = String(UUID) + "|" + String(measurementCount) + "|";
for (int i = 0; i < measurementCount; i++) {
  message += String(readT[i]) + "|" + String(readH[i]) + "|" + String(readD[i]) + "|";
}
message.trim();

uint16_t crc = calcCRC((uint8_t*)message.c_str(), message.length());
String fullMessage = "<|CRC|>" + String(crc) + "<|EOC|>" + message;


client.print(fullMessage);
Serial.print("Sent: ");
Serial.println(fullMessage);
Serial.println(crc);
Serial.println(message.c_str());
Serial.println(message.length());

    // Wait for acknowledgment
    while (client.connected()) {
      if (client.available()) {
        String response = client.readStringUntil('\n');
        Serial.print("Received: ");
        Serial.println(response);

        if (response == "<|ACK|>") {
          Serial.println("Acknowledgment received!");
          measurementCount = 0;
          currentCount = 0;
          break;
        }       
      }
    }
    delay(10000);
  }