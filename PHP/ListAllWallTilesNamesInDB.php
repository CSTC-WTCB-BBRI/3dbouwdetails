<?php

$tilesTable = $_POST["tilesTableName"];;

try
{
	$bdd = new PDO('mysql:host=localhost;dbname=tpdemo;charset=utf8', 'root', '', array(PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION, PDO::MYSQL_ATTR_LOCAL_INFILE => true));
}
catch(Exception $e)
{
	die('Error : ' . $e->getMessage());
}

$getName = "SELECT `libelle` FROM `" . $tilesTable . "` WHERE `mur`=1";
$result = $bdd->query($getName);

if ($result->errorCode() == 00000) 
{
  echo "Listing of all wall tiles names: OK\r\n";
} 
else 
{
  echo "Error while listing the wall tiles names!\r\n";
}

if ($result->rowCount() > 0) {
    echo "RETURNS\r\n";
}

// output data of each row
while($row = $result->fetch(PDO::FETCH_ASSOC)) {
    echo ";" . $row['libelle'];
} 

//Close the query access
$result->closeCursor();
?>