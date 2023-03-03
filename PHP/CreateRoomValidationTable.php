<?php

$clientId = $_POST["clientId"];
$projectId = $_POST["projectId"];

try
{
	$bdd = new PDO('mysql:host=localhost;dbname=tpdemo;charset=utf8', 'root', '', array(PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION, PDO::MYSQL_ATTR_LOCAL_INFILE => true));
}
catch(Exception $e)
{
	die('Error : ' . $e->getMessage());
}

$createCmd = "CREATE TABLE IF NOT EXISTS c" . $clientId . "_p" . $projectId . "_validatedRooms ( room_name VARCHAR(50) NOT NULL, validated TINYINT, PRIMARY KEY (room_name) ) CHARACTER SET 'utf8' ENGINE=INNODB;";

$result = $bdd->query($createCmd);
if ($result->errorCode() == 00000) 
{
  echo "Validated rooms table (re-)creation: OK\r\n";
} 
else 
{
  echo "Error during validated rooms table (re-)creation!\r\n";
}

//Close the query access
$result->closeCursor();

$insertCmd = "INSERT IGNORE INTO c" . $clientId . "_p" . $projectId . "_validatedRooms ( room_name, validated ) VALUES";

$rooms = array();
$count = 0;

foreach ($_POST["rooms"] as $key => $value) {
	$rooms[] = $value;
	$count += 1;
}

$newCount = 0;
while ($newCount < $count) {
	if ($newCount > 0) {
		$insertCmd = $insertCmd . ", ";
	}
	$insertCmd = $insertCmd . "( '" . $rooms[$newCount] . "', '0')";
	$newCount += 1;
}

$insertCmd = $insertCmd . ";";

$result = $bdd->query($insertCmd);
if ($result->errorCode() == 00000) 
{
  echo "Validated rooms table filling: OK\r\n";
} 
else 
{
  echo "Error during validated rooms table filling!\r\n";
}

//Close the query access
$result->closeCursor();

?>