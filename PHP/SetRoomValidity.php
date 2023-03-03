<?php

$clientId = $_POST["clientId"];
$projectId = $_POST["projectId"];
$room_name = $_POST["room_name"];
$validity = $_POST["validity"];

try
{
	$bdd = new PDO('mysql:host=localhost;dbname=tpdemo;charset=utf8', 'root', '', array(PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION, PDO::MYSQL_ATTR_LOCAL_INFILE => true));
}
catch(Exception $e)
{
	die('Error : ' . $e->getMessage());
}

$insertCmd = "REPLACE INTO c" . $clientId . "_p" . $projectId . "_validatedRooms (room_name, validated) VALUES";
$insertCmd = $insertCmd . "( '" . $room_name . "', '" . $validity . "')";
$result = $bdd->query($insertCmd);

if ($result->errorCode() == 00000) 
{
  echo "Room validity set: OK\r\n";
} 
else 
{
  echo "Error while setting room validity!\r\n";
}

$result->closeCursor();
?>