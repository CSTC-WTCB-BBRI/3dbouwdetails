<?php

$session = $_POST["session"];
$clientId = $_POST["clientId"];
$projectId = $_POST["projectId"];
$filename =  $_POST["filename"];
$surfaceID = $_POST["surfaceID"];
$positionX = $_POST["positionX"];
$positionY = $_POST["positionY"];
$positionZ = $_POST["positionZ"];
$rotationX = $_POST["rotationX"];
$rotationY = $_POST["rotationY"];
$rotationZ = $_POST["rotationZ"];

try
{
	$bdd = new PDO('mysql:host=localhost;dbname=tpdemo;charset=utf8', 'root', '', array(PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION, PDO::MYSQL_ATTR_LOCAL_INFILE => true));
}
catch(Exception $e)
{
	die('Error : ' . $e->getMessage());
}

// Create the table
$tableCreation = "CREATE TABLE IF NOT EXISTS c" . $clientId . "_p" . $projectId . "_screenshots ( id_surface INT UNSIGNED NOT NULL, filename VARCHAR(50), positionX FLOAT NOT NULL, positionY FLOAT NOT NULL, positionZ FLOAT NOT NULL, rotationX FLOAT NOT NULL, rotationY FLOAT NOT NULL, rotationZ FLOAT NOT NULL, session DATETIME, PRIMARY KEY (id_surface, session) ) CHARACTER SET 'utf8' ENGINE=INNODB;";
$result = $bdd->query($tableCreation);

if ($result->errorCode() == 00000) 
{
  echo "Screenshot table creation: OK\r\n";
} 
else 
{
  echo "Error while creating the screenshot table!\r\n";
}
$result->closeCursor();

//Insert comment
$insertCmd = "REPLACE INTO c" . $clientId . "_p" . $projectId . "_screenshots (id_surface, filename, positionX, positionY, positionZ, rotationX, rotationY, rotationZ, session) VALUES ( '" . $surfaceID . "', '" . $filename . "', '" . $positionX . "', '" . $positionY . "', '" . $positionZ . "', '" . $rotationX . "', '" . $rotationY . "', '" . $rotationZ . "', '" . $session . "');";

$result = $bdd->query($insertCmd);

if ($result->errorCode() == 00000) 
{
  echo "Screenshot insertion: OK\r\n";
} 
else 
{
  echo "Error while inserting screenshot!\r\n";
}
$result->closeCursor();
?>