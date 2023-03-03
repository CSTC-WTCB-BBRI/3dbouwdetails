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

$createCmd = "CREATE TABLE IF NOT EXISTS c" . $clientId . "_p" . $projectId . "_validatedSurfaces ( id_surface INT NOT NULL, validated TINYINT, PRIMARY KEY (id_surface) ) CHARACTER SET 'utf8' ENGINE=INNODB;";

$result = $bdd->query($createCmd);
if ($result->errorCode() == 00000) 
{
  echo "Validated surfaces table (re-)creation: OK\r\n";
} 
else 
{
  echo "Error during validated surfaces table (re-)creation!\r\n";
}

//Close the query access
$result->closeCursor();

?>