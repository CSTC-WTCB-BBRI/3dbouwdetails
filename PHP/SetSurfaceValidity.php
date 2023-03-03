<?php

$clientId = $_POST["clientId"];
$projectId = $_POST["projectId"];
$surfaceId = $_POST["surfaceId"];
$validity = $_POST["validity"];

try
{
	$bdd = new PDO('mysql:host=localhost;dbname=tpdemo;charset=utf8', 'root', '', array(PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION, PDO::MYSQL_ATTR_LOCAL_INFILE => true));
}
catch(Exception $e)
{
	die('Error : ' . $e->getMessage());
}

$insertCmd = "REPLACE INTO c" . $clientId . "_p" . $projectId . "_validatedSurfaces (id_surface, validated) VALUES";
$insertCmd = $insertCmd . "( '" . $surfaceId . "', '" . $validity . "')";
$result = $bdd->query($insertCmd);

if ($result->errorCode() == 00000) 
{
  echo "Surface validity set: OK\r\n";
} 
else 
{
  echo "Error while setting surface validity!\r\n";
}

$result->closeCursor();
?>